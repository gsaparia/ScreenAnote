using System.Runtime.InteropServices;
namespace ScreenAnote;
// A narrow, synchronous wrapper around Windows Media Foundation. Slot numbers
// follow mfobjects.h / mfreadwrite.h (IUnknown's three slots are included).
// No managed COM inheritance, third-party codecs, or unmanaged buffers escape.
internal sealed unsafe class Mp4Writer:IDisposable
{
    private nint writer;
    private int stream,audioStream=-1;
    private bool started,comInitialized,finished;
    private readonly int bytes;
    private static readonly Guid Major=new("48eba18e-f8c9-4687-bf11-0a74c9f96a8f"),Subtype=new("f7e34c9a-42e8-4714-b74b-cb29d72c35e5"),Video=new("73646976-0000-0010-8000-00aa00389b71"),H264=new("34363248-0000-0010-8000-00aa00389b71"),Nv12=new("3231564e-0000-0010-8000-00aa00389b71");
    private static readonly Guid Matrix=new("3e23d450-2c75-4d25-a00e-b91670d12327"),Range=new("c21b8ee5-b956-4071-8daf-325edf5cab11");
    private static readonly Guid FrameSize=new("1652c33d-d6b2-4012-b834-72030849a37d"),FrameRate=new("c459a2e8-3d2c-4e44-b132-fee5156c7bb0"),Aspect=new("c6376a1e-8d0a-4027-be45-6d9a0ad39bb6"),Interlace=new("e2724bb8-e676-4806-b4b2-a8d6efb44ccd"),Bitrate=new("20332624-fb0d-4d9e-bd0d-cbf6786c102e"),Stride=new("644b4e48-1e02-4516-b0eb-c01ca9d49ac6");
    public Mp4Writer(string path,int width,int height,bool audio=false)
    {
        bytes=checked(width*height*3/2);
        try
        {
            Check(CoInitializeEx(0,0));comInitialized=true;Check(MFStartup(0x20070,0));started=true;
            Check(MFCreateSinkWriterFromURL(path,0,0,out writer));
            nint output=0,input=0;
            try
            {
                Check(MFCreateMediaType(out output));Configure(output,H264,width,height);Set32(output,Bitrate,4_000_000);
                int index=0;Check(((delegate* unmanaged[Stdcall]<nint,nint,int*,int>)Slot(writer,3))(writer,output,&index));stream=index;
                Check(MFCreateMediaType(out input));Configure(input,Nv12,width,height);Set32(input,Stride,(uint)width);
                Check(((delegate* unmanaged[Stdcall]<nint,int,nint,nint,int>)Slot(writer,4))(writer,stream,input,0));
                if(audio)ConfigureAudio();
                Check(((delegate* unmanaged[Stdcall]<nint,int>)Slot(writer,5))(writer));
            }
            finally{Release(ref input);Release(ref output);}
        }
        catch{Dispose();throw;}
    }
    private static void Configure(nint type,Guid subtype,int width,int height)
    {
        SetGuid(type,Major,Video);SetGuid(type,Subtype,subtype);Set32(type,Interlace,2);Set32(type,Matrix,2);Set32(type,Range,2);
        Set64(type,FrameSize,((ulong)(uint)width<<32)|(uint)height);Set64(type,FrameRate,((ulong)VideoFrames.Fps<<32)|1);Set64(type,Aspect,(1UL<<32)|1);
    }
    private static readonly Guid Audio=new("73647561-0000-0010-8000-00aa00389b71"),Aac=new("00001610-0000-0010-8000-00aa00389b71"),Pcm=new("00000001-0000-0010-8000-00aa00389b71"),Channels=new("37e48bf5-645e-4c5b-89de-ada9e29b696a"),Rate=new("5faeeae7-0290-4c31-9e8a-c534f68d9dba"),Bits=new("f2deb57f-40fa-4764-aa33-ed4f2d1ff669"),AvgBytes=new("1aab75c8-cfef-451c-ab95-ac034b8e1731"),Alignment=new("322de230-9eeb-43bd-ab7a-ff412251541d");
    private void ConfigureAudio()
    {
        nint output=0,input=0;
        try
        {
            Check(MFCreateMediaType(out output));SetGuid(output,Major,Audio);SetGuid(output,Subtype,Aac);Set32(output,Channels,1);Set32(output,Rate,AudioFrames.SampleRate);Set32(output,Bits,16);Set32(output,AvgBytes,12000);
            int index=0;Check(((delegate* unmanaged[Stdcall]<nint,nint,int*,int>)Slot(writer,3))(writer,output,&index));audioStream=index;
            Check(MFCreateMediaType(out input));SetGuid(input,Major,Audio);SetGuid(input,Subtype,Pcm);Set32(input,Channels,1);Set32(input,Rate,AudioFrames.SampleRate);Set32(input,Bits,16);Set32(input,Alignment,2);Set32(input,AvgBytes,AudioFrames.SampleRate*2);
            Check(((delegate* unmanaged[Stdcall]<nint,int,nint,nint,int>)Slot(writer,4))(writer,audioStream,input,0));
        }
        finally{Release(ref input);Release(ref output);}
    }
    public void Write(byte[] frame,long time,long duration)
    {
        if(frame.Length!=bytes)throw new ArgumentException("Invalid video frame length.");WriteSample(stream,frame,time,duration);
    }
    public void WriteAudio(byte[] pcm,long firstSample)
    {
        if(audioStream<0||pcm.Length<2||pcm.Length%2!=0)throw new ArgumentException("Invalid audio stream or sample.");
        long end=firstSample+pcm.Length/2;WriteSample(audioStream,pcm,AudioFrames.Ticks(firstSample),AudioFrames.Ticks(end)-AudioFrames.Ticks(firstSample));
    }
    private void WriteSample(int stream,byte[] frame,long time,long duration)
    {
        if(finished||writer==0)throw new InvalidOperationException("Recorder has stopped.");
        int bytes=frame.Length;
        nint buffer=0,sample=0;
        try
        {
            Check(MFCreateMemoryBuffer(bytes,out buffer));nint destination=0;int max=0,length=0;
            Check(((delegate* unmanaged[Stdcall]<nint,nint*,int*,int*,int>)Slot(buffer,3))(buffer,&destination,&max,&length));
            try{Marshal.Copy(frame,0,destination,bytes);}finally{Check(((delegate* unmanaged[Stdcall]<nint,int>)Slot(buffer,4))(buffer));}
            Check(((delegate* unmanaged[Stdcall]<nint,int,int>)Slot(buffer,6))(buffer,bytes));
            Check(MFCreateSample(out sample));
            Check(((delegate* unmanaged[Stdcall]<nint,nint,int>)Slot(sample,42))(sample,buffer));
            Check(((delegate* unmanaged[Stdcall]<nint,long,int>)Slot(sample,36))(sample,time));
            Check(((delegate* unmanaged[Stdcall]<nint,long,int>)Slot(sample,38))(sample,Math.Max(1,duration)));
            Check(((delegate* unmanaged[Stdcall]<nint,int,nint,int>)Slot(writer,6))(writer,stream,sample));
        }
        finally{Release(ref sample);Release(ref buffer);}
    }
    public void Complete(){if(finished)return;Check(((delegate* unmanaged[Stdcall]<nint,int>)Slot(writer,11))(writer));finished=true;}
    public void Dispose(){Release(ref writer);if(started){MFShutdown();started=false;}if(comInitialized){CoUninitialize();comInitialized=false;}}
    private static nint Slot(nint instance,int index)=>Marshal.ReadIntPtr(Marshal.ReadIntPtr(instance),index*IntPtr.Size);
    private static void SetGuid(nint p,Guid key,Guid value)=>Check(((delegate* unmanaged[Stdcall]<nint,Guid*,Guid*,int>)Slot(p,24))(p,&key,&value));
    private static void Set32(nint p,Guid key,uint value)=>Check(((delegate* unmanaged[Stdcall]<nint,Guid*,uint,int>)Slot(p,21))(p,&key,value));
    private static void Set64(nint p,Guid key,ulong value)=>Check(((delegate* unmanaged[Stdcall]<nint,Guid*,ulong,int>)Slot(p,22))(p,&key,value));
    private static void Release(ref nint p){if(p==0)return;((delegate* unmanaged[Stdcall]<nint,uint>)Slot(p,2))(p);p=0;}
    private static void Check(int hr){if(hr<0)Marshal.ThrowExceptionForHR(hr);}
    [DllImport("ole32.dll")]private static extern int CoInitializeEx(nint reserved,uint mode);
    [DllImport("ole32.dll")]private static extern void CoUninitialize();
    [DllImport("mfplat.dll",ExactSpelling=true)]private static extern int MFStartup(int version,int flags);
    [DllImport("mfplat.dll",ExactSpelling=true)]private static extern int MFShutdown();
    [DllImport("mfplat.dll",ExactSpelling=true)]private static extern int MFCreateMediaType(out nint type);
    [DllImport("mfplat.dll",ExactSpelling=true)]private static extern int MFCreateMemoryBuffer(int size,out nint buffer);
    [DllImport("mfplat.dll",ExactSpelling=true)]private static extern int MFCreateSample(out nint sample);
    [DllImport("mfreadwrite.dll",CharSet=CharSet.Unicode,ExactSpelling=true)]private static extern int MFCreateSinkWriterFromURL(string url,nint byteStream,nint attributes,out nint writer);
}
