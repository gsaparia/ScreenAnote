using System.Buffers.Binary;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using ScreenAnote;
internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        string path=Path.Combine(Path.GetTempPath(),"ScreenAnoteSmoke_"+Guid.NewGuid().ToString("N")+".mp4");
        try
        {
            // Native H.264 path exercises every COM vtable call, buffer lifetime,
            // time/duration setter and MP4 finalization with 15 synthetic frames.
            var rgb=new byte[320*240*4];var yuv=new byte[320*240*3/2];
            using(var writer=new Mp4Writer(path,320,240,true))
            {
                for(int frame=0;frame<15;frame++)
                {
                    for(int y=0;y<240;y++)for(int x=0;x<320;x++){int i=(y*320+x)*4;rgb[i]=(byte)x;rgb[i+1]=(byte)y;rgb[i+2]=(byte)(frame*17);rgb[i+3]=255;}
                    VideoFrames.ToNv12(rgb,yuv,320,240);writer.Write(yuv,frame*VideoFrames.FrameTicks,VideoFrames.FrameTicks);
                    var tone=new byte[2940*2];for(int sample=0;sample<2940;sample++){short value=(short)(Math.Sin((frame*2940+sample)*Math.PI*2*440/44100)*6000);System.Buffers.Binary.BinaryPrimitives.WriteInt16LittleEndian(tone.AsSpan(sample*2,2),value);}writer.WriteAudio(tone,frame*2940);
                }
                writer.Complete();
            }
            var bytes=File.ReadAllBytes(path);var boxes=new HashSet<string>();
            for(int pos=0;pos+8<=bytes.Length;)
            {
                ulong size=BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(pos,4));string name=Encoding.ASCII.GetString(bytes,pos+4,4);
                if(size==1){if(pos+16>bytes.Length)throw new Exception("Truncated MP4 atom");size=BinaryPrimitives.ReadUInt64BigEndian(bytes.AsSpan(pos+8,8));}
                if(size==0)size=(ulong)(bytes.Length-pos);if(size<8||size>(ulong)(bytes.Length-pos))throw new Exception("Invalid MP4 atom length");boxes.Add(name);pos+=checked((int)size);
            }
            if(!boxes.IsSupersetOf(new[]{"ftyp","mdat","moov"})||bytes.Length<1000)throw new Exception("Missing finalized MP4 structure");
            var container=Encoding.Latin1.GetString(bytes);if(!container.Contains("avc1")||!container.Contains("mp4a"))throw new Exception("Missing H.264 / AAC sample descriptions");
            Console.WriteLine("PASS: Windows native H.264 + AAC encoder and finalized MP4 audio/video container.");
            if(args.Contains("--microphone"))
            {
                Console.WriteLine("Speak for two seconds: testing default microphone capture.");using var microphone=new WaveMicrophone(true,()=>false);bool sound=false;
                for(int i=0;i<100;i++){Thread.Sleep(20);if(microphone.TakeError() is string error)throw new Exception(error);if(microphone.Read(882).Any(b=>b!=0))sound=true;}
                if(!sound)throw new Exception("No microphone sound received. Speak, select an input device, and check Windows permissions.");microphone.Enabled=false;if(microphone.Read(100).Any(b=>b!=0))throw new Exception("Mic mute failed");Console.WriteLine("PASS: microphone input and mute.");
            }
            if(args.Contains("--systemaudio"))
            {
                Console.WriteLine("Play speaker audio now: testing loopback capture for two seconds.");using var speaker=new SystemAudio(true,()=>false);bool sound=false;
                for(int i=0;i<100;i++){Thread.Sleep(20);if(speaker.TakeError() is string error)throw new Exception(error);if(speaker.Read(882).Any(b=>b!=0))sound=true;}
                if(!sound)throw new Exception("No system audio received. Play sound on the default playback device.");speaker.Enabled=false;if(speaker.Read(100).Any(b=>b!=0))throw new Exception("Speaker mute failed");Console.WriteLine("PASS: system loopback and mute.");
            }
            if(args.Contains("--captureframes"))
            {
                foreach(var display in Screen.AllScreens)
                {
                    var region=new Rectangle(display.Bounds.Location,new Size(Math.Min(1248,display.Bounds.Width),Math.Min(715,display.Bounds.Height)));
                    using var frame=new Bitmap(region.Width,region.Height,System.Drawing.Imaging.PixelFormat.Format32bppRgb);
                    for(int i=0;i<100;i++)RecordingScreenCapture.CopyFrame(frame,region);
                    Console.WriteLine("PASS: 100 native capture frames on "+display.DeviceName+" "+region);
                }
            }
            if(args.Contains("--recorderstop"))
            {
                foreach(bool audio in new[]{false,true})
                {
                    var recorder=new RegionRecorder(new Rectangle(0,0,320,240),false);
                    try
                    {
                        recorder.Ready.WaitAsync(TimeSpan.FromSeconds(10)).GetAwaiter().GetResult();
                        // Enable from this STA caller to exercise the old UI-thread path.
                        recorder.MicrophoneEnabled=audio;Thread.Sleep(700);recorder.TogglePause();Thread.Sleep(100);recorder.TogglePause();Thread.Sleep(300);
                        recorder.StopAsync().WaitAsync(TimeSpan.FromSeconds(15)).GetAwaiter().GetResult();
                        if(!File.Exists(recorder.FilePath)||new FileInfo(recorder.FilePath).Length<1000)throw new Exception("Recording did not finalize");
                        Console.WriteLine("PASS: live recorder Stop/pause/resume with audio "+(audio?"on":"off"));
                    }
                    finally{if(recorder.Completion.IsCompleted)recorder.Dispose();}
                }
            }
            using(var parent=new Panel{BackColor=Color.FromArgb(242,245,250),Size=new Size(80,60)})
            using(var button=new ModernButton{Text="",Symbol="pen",Size=new Size(60,40)})
            using(var rendered=new Bitmap(60,40))
            {
                parent.Controls.Add(button);button.DrawToBitmap(rendered,new Rectangle(0,0,60,40));
                if(rendered.GetPixel(0,0).ToArgb()!=parent.BackColor.ToArgb())throw new Exception("Button corner background artifact");
            }
            using(var rendered=new Bitmap(100,60))
            {
                var text=new Annotation{Kind=EditTool.Text,Text="Text",Bounds=new RectangleF(10,10,80,40),TextBackground=Color.Yellow};
                using(var graphics=Graphics.FromImage(rendered)){graphics.Clear(Color.Transparent);text.Draw(graphics);}
                if(rendered.GetPixel(88,48).ToArgb()!=Color.Yellow.ToArgb())throw new Exception("Text background rendering failed");
                text.TextBackground=Color.Transparent;using(var graphics=Graphics.FromImage(rendered)){graphics.Clear(Color.Transparent);text.Draw(graphics);}
                if(rendered.GetPixel(88,48).A!=0)throw new Exception("Transparent background rendering failed");
            }
            Console.WriteLine("PASS: button corner paint and solid/transparent text background rendering.");
            using var image=new Bitmap(16,16);image.SetPixel(4,4,Color.FromArgb(128,200,40,10));
            var item=new Annotation{Kind=EditTool.ImageSticker,Bounds=new RectangleF(1,2,16,16),Asset=image};var restored=AnnotationTransfer.Deserialize(AnnotationTransfer.Serialize(item));
            using var restoredImage=restored.Asset;if(restoredImage==null||restoredImage.GetPixel(4,4).ToArgb()!=image.GetPixel(4,4).ToArgb())throw new Exception("PNG asset clipboard serialization failed");
            Console.WriteLine("PASS: Windows bitmap serialization preserves transparent sticker pixels.");
            Console.WriteLine("UI, clipboard and region recording checks remain in VALIDATION.md. Synthetic test MP4 removed.");return 0;
        }
        catch(Exception error){Console.Error.WriteLine(error);return 1;}
        finally{File.Delete(path);}
    }
}
