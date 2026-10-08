using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System.Buffers.Binary;
namespace ScreenAnote;
// Device creation and teardown belong to a background owner thread. In particular,
// never join WASAPI while holding the lock used by its DataAvailable callback.
internal sealed class SystemAudio:IDisposable
{
    private readonly CancellationTokenSource stop=new();
    private readonly Task worker;
    private readonly Func<IWaveIn> createDevice;
    private readonly object gate=new();
    private readonly Func<bool> paused;
    private BufferedWaveProvider? buffer;
    private ISampleProvider? samples;
    private volatile bool enabled;
    private string? error;
    private int stopWaited,disposed;
    public SystemAudio(bool enabled,Func<bool> paused,Func<IWaveIn>? createDevice=null)
    {
        this.enabled=enabled;this.paused=paused;this.createDevice=createDevice??(()=>new WasapiLoopbackCapture());
        worker=Task.Factory.StartNew(Run,CancellationToken.None,TaskCreationOptions.LongRunning,TaskScheduler.Default);
    }
    public bool Enabled{get=>enabled;set{if(stop.IsCancellationRequested)return;enabled=value;Discard();}}
    public void Discard(){lock(gate)buffer?.ClearBuffer();}
    public string? TakeError()=>Interlocked.Exchange(ref error,null);
    private void Run()
    {
        IWaveIn? device=null;
        void ReleaseDevice()
        {
            var old=device;device=null;
            // Detach shared buffers first; disposal must happen outside gate.
            lock(gate){buffer=null;samples=null;}
            old?.Dispose();
        }
        try
        {
            while(!stop.IsCancellationRequested)
            {
                if(!enabled){if(device!=null)ReleaseDevice();stop.Token.WaitHandle.WaitOne(10);continue;}
                if(device==null)
                {
                    try
                    {
                        device=createDevice();
                        var incoming=new BufferedWaveProvider(device.WaveFormat){BufferDuration=TimeSpan.FromSeconds(2),DiscardOnBufferOverflow=true,ReadFully=true};
                        ISampleProvider source=incoming.ToSampleProvider();
                        if(source.WaveFormat.Channels==2)source=new StereoToMonoSampleProvider(source){LeftVolume=.5f,RightVolume=.5f};
                        else if(source.WaveFormat.Channels!=1){var mono=new MultiplexingSampleProvider(new[]{source},1);for(int ch=0;ch<source.WaveFormat.Channels;ch++)mono.ConnectInputToOutput(ch,0);source=mono;}
                        if(source.WaveFormat.SampleRate!=AudioFrames.SampleRate)source=new WdlResamplingSampleProvider(source,AudioFrames.SampleRate);
                        lock(gate){buffer=incoming;samples=source;}
                        device.DataAvailable+=(_,e)=>{lock(gate){if(enabled&&!stop.IsCancellationRequested&&!paused()&&ReferenceEquals(buffer,incoming))incoming.AddSamples(e.Buffer,0,e.BytesRecorded);}};
                        device.RecordingStopped+=(_,e)=>{if(e.Exception!=null){enabled=false;Interlocked.Exchange(ref error,"Speaker audio unavailable: "+e.Exception.Message);}};
                        device.StartRecording();
                    }
                    catch(Exception ex){enabled=false;Interlocked.Exchange(ref error,"Speaker audio unavailable: "+ex.Message);ReleaseDevice();}
                }
                stop.Token.WaitHandle.WaitOne(10);
            }
        }
        catch(Exception ex){Interlocked.Exchange(ref error,"Speaker audio shutdown: "+ex.Message);}
        finally{try{ReleaseDevice();}catch(Exception ex){Interlocked.Exchange(ref error,"Speaker audio shutdown: "+ex.Message);}}
    }
    public byte[] Read(int count)
    {
        var result=new byte[count*2];lock(gate)
        {
            if(!enabled||paused()||samples==null)return result;
            var values=new float[count];int read=samples.Read(values,0,count);
            for(int i=0;i<read;i++)BinaryPrimitives.WriteInt16LittleEndian(result.AsSpan(i*2,2),(short)Math.Clamp(values[i]*32767,short.MinValue,short.MaxValue));
        }
        return result;
    }
    public void StopCapture()
    {
        stop.Cancel();
        if(Interlocked.Exchange(ref stopWaited,1)==0&&!worker.Wait(TimeSpan.FromSeconds(3)))
            Interlocked.Exchange(ref error,"Speaker device cleanup is taking longer; MP4 finalization will continue.");
    }
    public void Dispose()
    {
        if(Interlocked.Exchange(ref disposed,1)!=0)return;
        StopCapture();
        // A slow driver retains its buffers until the owner has actually exited.
        // Do not free native capture memory or dispose the token under that thread.
        _=worker.ContinueWith(_=>stop.Dispose(),CancellationToken.None,TaskContinuationOptions.ExecuteSynchronously,TaskScheduler.Default);
    }
}
