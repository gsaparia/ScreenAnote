using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
namespace ScreenAnote;
internal sealed class RegionRecorder:IDisposable
{
    private readonly CancellationTokenSource stop=new();
    private readonly TaskCompletionSource ready=new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly RecordingClock clock=new(RecordingClock.Now);
    private readonly Task worker;
    private WaveMicrophone? microphone;private volatile bool microphoneEnabled;
    private string? microphoneError;
    private long stoppedAt=-1;
    private long ActiveTicks{get{long value=Interlocked.Read(ref stoppedAt);return value>=0?value:clock.Elapsed(RecordingClock.Now);}}
    public bool MicrophoneEnabled{get=>microphoneEnabled;set{microphoneEnabled=value;if(microphone!=null)microphone.Enabled=value;}}
    public string? TakeMicrophoneError()=>Interlocked.Exchange(ref microphoneError,null);
    public string FilePath{get;}
    public Size Output{get;}
    public bool Paused=>clock.Paused;
    public TimeSpan Duration=>TimeSpan.FromTicks(ActiveTicks);
    public Task Ready=>ready.Task;
    public Task Completion=>worker;
    public void TogglePause(){clock.SetPaused(!Paused,RecordingClock.Now);microphone?.Discard();}
    public RegionRecorder(Rectangle region,bool microphoneEnabled=false)
    {
        this.microphoneEnabled=microphoneEnabled;
        Output=VideoFrames.OutputSize(region.Size);
        FilePath=Path.Combine(Path.GetTempPath(),"ScreenAnote_"+Guid.NewGuid().ToString("N")+".mp4");
        worker=Task.Factory.StartNew(()=>Record(region),CancellationToken.None,TaskCreationOptions.LongRunning,TaskScheduler.Default);
    }
    private void Record(Rectangle region)
    {
        try
        {
            using var encoder=new Mp4Writer(FilePath,Output.Width,Output.Height,true);
            using var capture=new Bitmap(region.Width,region.Height,PixelFormat.Format32bppRgb);
            using var frame=new Bitmap(Output.Width,Output.Height,PixelFormat.Format32bppRgb);
            using var screen=Graphics.FromImage(capture);using var resized=Graphics.FromImage(frame);
            resized.InterpolationMode=InterpolationMode.HighQualityBilinear;
            var rgb=new byte[Output.Width*Output.Height*4];var pending=new byte[Output.Width*Output.Height*3/2];var next=new byte[pending.Length];
            bool hasFrame=false;long previous=0;var stopwatch=Stopwatch.StartNew();double due=0;
            clock.Reset(RecordingClock.Now);using var mic=new WaveMicrophone(microphoneEnabled,()=>Paused);microphone=mic;mic.Enabled=microphoneEnabled;
            long audioWritten=0;
            void WriteAudioUntil(long ticks)
            {
                long target=AudioFrames.Samples(ticks);
                while(audioWritten<target){int count=(int)Math.Min(AudioFrames.ChunkSamples,target-audioWritten);encoder.WriteAudio(mic.Read(count),audioWritten);audioWritten+=count;}
                if(mic.TakeError() is string error){microphoneEnabled=false;microphoneError=error;}
            }
            ready.TrySetResult();
            while(!stop.IsCancellationRequested)
            {
                if(Paused){stop.Token.WaitHandle.WaitOne(30);due=stopwatch.Elapsed.TotalMilliseconds;continue;}
                screen.CopyFromScreen(region.Location,Point.Empty,region.Size,CopyPixelOperation.SourceCopy);
                resized.DrawImage(capture,new Rectangle(Point.Empty,Output));
                var data=frame.LockBits(new Rectangle(Point.Empty,Output),ImageLockMode.ReadOnly,PixelFormat.Format32bppRgb);
                try{for(int y=0;y<Output.Height;y++)Marshal.Copy(data.Scan0+y*data.Stride,rgb,y*Output.Width*4,Output.Width*4);}finally{frame.UnlockBits(data);}
                VideoFrames.ToNv12(rgb,next,Output.Width,Output.Height);
                long now=ActiveTicks;
                if(hasFrame){now=Math.Max(previous+1,now);encoder.Write(pending,previous,now-previous);}else now=0;
                WriteAudioUntil(Math.Max(0,now-TimeSpan.TicksPerMillisecond*50));
                (pending,next)=(next,pending);hasFrame=true;previous=now;
                due+=1000d/VideoFrames.Fps;int wait=(int)Math.Max(0,due-stopwatch.Elapsed.TotalMilliseconds);if(wait>0)stop.Token.WaitHandle.WaitOne(wait);else due=stopwatch.Elapsed.TotalMilliseconds;
            }
            mic.StopCapture();long endTime=Math.Max(previous+VideoFrames.FrameTicks,ActiveTicks);
            if(hasFrame){encoder.Write(pending,previous,endTime-previous);WriteAudioUntil(endTime);}
            microphone=null;
            encoder.Complete();
        }
        catch(Exception error){ready.TrySetException(error);throw;}
    }
    public async Task StopAsync(){Interlocked.CompareExchange(ref stoppedAt,clock.Elapsed(RecordingClock.Now),-1);stop.Cancel();await worker;}
    public void Dispose()
    {
        // RecorderForm awaits StopAsync before disposing; all COM/GDI objects stay
        // on the encoding thread. Delete only this session's temporary file.
        if(!worker.IsCompleted)throw new InvalidOperationException("Stop recording before disposing it.");
        stop.Dispose();try{File.Delete(FilePath);}catch(IOException){}catch(UnauthorizedAccessException){}
    }
}
