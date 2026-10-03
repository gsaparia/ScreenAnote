using System.Runtime.InteropServices;
using System.Text;
namespace ScreenAnote;
// Default Windows input device, 44.1 kHz mono PCM16. Polls prepared WinMM
// buffers on its own thread so video encoding cannot block microphone capture.
internal sealed class WaveMicrophone:IDisposable
{
    private readonly CancellationTokenSource stop=new();
    private readonly PcmQueue queue=new();
    private readonly Task worker;
    private readonly Func<bool> paused;
    private volatile bool enabled;
    private string? error;
    private int generation;
    public bool Enabled{get=>enabled;set{enabled=value;Discard();}}
    public void Discard(){Interlocked.Increment(ref generation);queue.Clear();}
    public string? TakeError()=>Interlocked.Exchange(ref error,null);
    public byte[] Read(int samples)=>Enabled&&!paused()?queue.Read(samples):new byte[samples*2];
    public WaveMicrophone(bool enabled,Func<bool> paused)
    {
        this.enabled=enabled;this.paused=paused;
        worker=Task.Factory.StartNew(Run,CancellationToken.None,TaskCreationOptions.LongRunning,TaskScheduler.Default);
    }
    private void Run()
    {
        Device? device=null;int observed=-1;
        try
        {
            while(!stop.IsCancellationRequested)
            {
                int current=Volatile.Read(ref generation);if(current!=observed){device?.Dispose();device=null;queue.Clear();observed=current;}
                if(!enabled||paused()){device?.Dispose();device=null;queue.Clear();stop.Token.WaitHandle.WaitOne(10);continue;}
                try{device??=new Device();foreach(var block in device.Drain())if(enabled&&!paused()&&current==Volatile.Read(ref generation))queue.Add(block);}
                catch(Exception ex){enabled=false;queue.Clear();error="Microphone unavailable: "+ex.Message;device?.Dispose();device=null;}
                stop.Token.WaitHandle.WaitOne(10);
            }
        }
        finally{device?.Dispose();}
    }
    public void StopCapture(){stop.Cancel();worker.GetAwaiter().GetResult();}
    public void Dispose(){StopCapture();stop.Dispose();queue.Clear();}
    [StructLayout(LayoutKind.Sequential,Pack=2)]private struct Format
    {
        public ushort Tag,Channels;public uint Rate,BytesPerSecond;public ushort Alignment,Bits,Extra;
    }
    [StructLayout(LayoutKind.Sequential)]private struct Header
    {
        public nint Data;public uint Length,Recorded;public nuint User;public uint Flags,Loops;public nint Next;public nuint Reserved;
    }
    private sealed class Device:IDisposable
    {
        private nint handle;
        private readonly List<(nint Header,nint Data)> buffers=new();
        private static readonly int HeaderSize=Marshal.SizeOf<Header>();
        private int next;
        public Device()
        {
            try
            {
                var format=new Format{Tag=1,Channels=1,Rate=AudioFrames.SampleRate,BytesPerSecond=AudioFrames.SampleRate*2,Alignment=2,Bits=16};
                Check(waveInOpen(out handle,uint.MaxValue,ref format,0,0,0));
                for(int i=0;i<16;i++)
                {
                    nint data=Marshal.AllocHGlobal(1764),header=Marshal.AllocHGlobal(HeaderSize);
                    Marshal.StructureToPtr(new Header{Data=data,Length=1764},header,false);buffers.Add((header,data));
                    Check(waveInPrepareHeader(handle,header,HeaderSize));Check(waveInAddBuffer(handle,header,HeaderSize));
                }
                Check(waveInStart(handle));
            }
            catch{Dispose();throw;}
        }
        public List<byte[]> Drain()
        {
            var result=new List<byte[]>();
            for(int count=0;count<buffers.Count;count++)
            {
                var entry=buffers[next];var header=Marshal.PtrToStructure<Header>(entry.Header);if((header.Flags&1)==0)break;
                if(header.Recorded>0){var bytes=new byte[header.Recorded&~1u];Marshal.Copy(entry.Data,bytes,0,bytes.Length);result.Add(bytes);}
                Check(waveInAddBuffer(handle,entry.Header,HeaderSize));next=(next+1)%buffers.Count;
            }
            return result;
        }
        public void Dispose()
        {
            if(handle!=0)waveInReset(handle);
            foreach(var entry in buffers)
            {
                var header=Marshal.PtrToStructure<Header>(entry.Header);
                // Never free a buffer while a driver still owns its header.
                if((header.Flags&2)!=0&&waveInUnprepareHeader(handle,entry.Header,HeaderSize)!=0)continue;
                Marshal.FreeHGlobal(entry.Header);Marshal.FreeHGlobal(entry.Data);
            }
            buffers.Clear();if(handle!=0){waveInClose(handle);handle=0;}
        }
    }
    private static void Check(uint result){if(result==0)return;var message=new StringBuilder(256);waveInGetErrorText(result,message,message.Capacity);throw new InvalidOperationException(message.Length>0?message.ToString():"Audio device error "+result);}
    [DllImport("winmm.dll")]private static extern uint waveInOpen(out nint handle,uint id,ref Format format,nint callback,nint instance,uint flags);
    [DllImport("winmm.dll")]private static extern uint waveInPrepareHeader(nint handle,nint header,int size);
    [DllImport("winmm.dll")]private static extern uint waveInUnprepareHeader(nint handle,nint header,int size);
    [DllImport("winmm.dll")]private static extern uint waveInAddBuffer(nint handle,nint header,int size);
    [DllImport("winmm.dll")]private static extern uint waveInStart(nint handle);
    [DllImport("winmm.dll")]private static extern uint waveInReset(nint handle);
    [DllImport("winmm.dll")]private static extern uint waveInClose(nint handle);
    [DllImport("winmm.dll",CharSet=CharSet.Unicode)]private static extern uint waveInGetErrorText(uint code,StringBuilder message,int capacity);
}
