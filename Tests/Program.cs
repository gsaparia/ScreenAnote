using System.Drawing;
using ScreenAnote;
static void Near(float actual,float expected){if(Math.Abs(actual-expected)>.001)throw new Exception($"Expected {expected}, actual {actual}");}
var original=new RectangleF(10,20,100,80);
var translated=ShapeGeometry.Map(new PointF(35,60),original,new RectangleF(30,50,100,80));Near(translated.X,55);Near(translated.Y,90);
var scaled=ShapeGeometry.Map(new PointF(35,60),original,new RectangleF(10,20,200,40));Near(scaled.X,60);Near(scaled.Y,40);
// Resizing each corner keeps the opposite corner fixed.
var tl=ShapeGeometry.Resize(original,0,new PointF(0,5));Near(tl.Right,110);Near(tl.Bottom,100);Near(tl.Left,0);Near(tl.Top,5);
var tr=ShapeGeometry.Resize(original,1,new PointF(150,5));Near(tr.Left,10);Near(tr.Bottom,100);Near(tr.Right,150);Near(tr.Top,5);
var br=ShapeGeometry.Resize(original,2,new PointF(150,120));Near(br.Left,10);Near(br.Top,20);Near(br.Right,150);Near(br.Bottom,120);
var bl=ShapeGeometry.Resize(original,3,new PointF(0,120));Near(bl.Right,110);Near(bl.Top,20);Near(bl.Left,0);Near(bl.Bottom,120);
var crossed=ShapeGeometry.Resize(original,0,new PointF(900,900));Near(crossed.Width,4);Near(crossed.Height,4);
// Reversed arrows preserve their direction when resized.
var head=ShapeGeometry.Map(new PointF(10,20),original,new RectangleF(0,0,200,160));var tail=ShapeGeometry.Map(new PointF(110,100),original,new RectangleF(0,0,200,160));Near(head.X,0);Near(tail.X,200);Near(tail.Y,160);
Near(ShapeGeometry.Distance(new PointF(5,3),new PointF(0,0),new PointF(10,0)),3);
Near(ShapeGeometry.Distance(new PointF(13,4),new PointF(0,0),new PointF(10,0)),5);
Near(ShapeGeometry.Distance(new PointF(3,4),PointF.Empty,PointF.Empty),5);
var reversedBox=ShapeGeometry.Box(new PointF(50,60),new PointF(10,20));Near(reversedBox.X,10);Near(reversedBox.Y,20);Near(reversedBox.Width,40);Near(reversedBox.Height,40);
var pointer=new PointF(400,350);var anchor=new PointF(120,80);var origin=new PointF(-50,-40);var priorPan=new PointF(30,20);float nextScale=2.5f;
var nextPan=ShapeGeometry.ZoomPan(pointer,anchor,nextScale,origin,priorPan);
Near(origin.X+nextPan.X-priorPan.X+anchor.X*nextScale,pointer.X);Near(origin.Y+nextPan.Y-priorPan.Y+anchor.Y*nextScale,pointer.Y);
Console.WriteLine("PASS: translation, scaling, four resize handles, inversion guard, reversed arrows, hit distances reverse-drag bounds and pointer-anchored zoom.");

foreach(var dimensions in new[]{new Size(864,520),new Size(1280,760),new Size(1920,1000)})
{
 var area=new Rectangle(Point.Empty,dimensions);var palette=CanvasLayout.Palette(dimensions);var zoom=CanvasLayout.Zoom(dimensions);var drawer=CanvasLayout.Drawer(dimensions);
 if(!area.Contains(palette)||!area.Contains(zoom)||!area.Contains(drawer))throw new Exception("Floating panel exceeds workspace");
 if(palette.IntersectsWith(zoom)||palette.IntersectsWith(drawer))throw new Exception("Palette overlaps navigation or stickers");
 foreach(var selected in new RectangleF?[]{null,new RectangleF(-400,-200,100,40),new RectangleF(dimensions.Width+100,dimensions.Height,100,40),new RectangleF(100,180,120,80)})
 foreach(bool open in new[]{false,true}){var context=CanvasLayout.Context(dimensions,open,selected);if(!area.Contains(context)||context.IntersectsWith(palette)||open&&context.IntersectsWith(drawer))throw new Exception("Context toolbar overlaps fixed overlays or exceeds workspace");}
}
if(StickerCatalog.Items.Count<120 || StickerCatalog.Items.Select(s=>s.Glyph).Distinct().Count()!=StickerCatalog.Items.Count)throw new Exception("Sticker catalogue incomplete or duplicated");
if(!StickerCatalog.Items.Any(s=>s.Name.Contains("arrow"))||!StickerCatalog.Items.Any(s=>s.Name.Contains("heart")))throw new Exception("Named sticker search data missing");
Console.WriteLine("PASS: three workspace sizes, offscreen selections, drawer collision avoidance and searchable sticker catalogue.");

var sequence=new CounterSequence();if(sequence.Take()!=1 || sequence.Take()!=2 || sequence.Next!=3)throw new Exception("Counter increment failure");sequence.Reset();if(sequence.Take()!=1)throw new Exception("Counter reset failure");sequence.Reset(9);if(sequence.Take()!=9)throw new Exception("Counter restart failure");
try{sequence.Reset(0);throw new Exception("Invalid counter accepted");}catch(ArgumentOutOfRangeException){}
var annotation=new Annotation{Kind=EditTool.Arrow,Bounds=new RectangleF(10,20,100,80),Color=Color.Red,Stroke=4,FontSize=24,Points=new(){new PointF(10,20),new PointF(110,100)}};
var clone=annotation.Clone();clone.Transform(new RectangleF(30,50,100,80));if(annotation.Points[0]!=new PointF(10,20)||annotation.Bounds.X!=10)throw new Exception("Duplicating mutated the original");Near(clone.Points[0].X,30);Near(clone.Points[0].Y,50);
foreach(var kind in new[]{EditTool.Text,EditTool.Arrow,EditTool.Rectangle,EditTool.Counter,EditTool.Pixelate,EditTool.Pen})
{annotation.Kind=kind;annotation.Text=kind==EditTool.Counter?"7":"Example";var restored=AnnotationTransfer.Deserialize(AnnotationTransfer.Serialize(annotation));if(restored.Kind!=kind||restored.Text!=annotation.Text||restored.Color.ToArgb()!=Color.Red.ToArgb()||restored.Points[1]!=annotation.Points[1])throw new Exception("Clipboard annotation roundtrip failed");}
try{AnnotationTransfer.Deserialize("{}");throw new Exception("Invalid clipboard accepted");}catch(InvalidOperationException){}
Console.WriteLine("PASS: counter increment/reset, independent duplicate transforms and clipboard roundtrips for six annotation kinds.");

foreach(float dpi in new[]{1f,1.5f,2f}){var logical=new Size(1440,800);var bounds=CanvasLayout.Scale(CanvasLayout.Palette(logical),dpi);Near(bounds.Width,CanvasLayout.Palette(logical).Width*dpi);Near(bounds.Y,CanvasLayout.Palette(logical).Y*dpi);}
Console.WriteLine("PASS: layout scaling at 100%, 150% and 200% DPI.");

var appPath=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../App/ScreenAnote.dll"));
if(File.Exists(appPath))
{
 var assembly=System.Reflection.Assembly.LoadFile(appPath);var resources=assembly.GetManifestResourceNames();
 using var stream=assembly.GetManifestResourceStream("ScreenAnote.Assets.Stickers.catalog.json")??throw new Exception("Embedded catalogue missing");
 using var doc=System.Text.Json.JsonDocument.Parse(stream);
 if(doc.RootElement.GetArrayLength()<350)throw new Exception("Embedded sticker count mismatch");
 foreach(var entry in doc.RootElement.EnumerateArray()){var key=entry.GetProperty("key").GetString();if(!resources.Contains("ScreenAnote.Assets.Stickers."+key+".png"))throw new Exception("Embedded PNG missing: "+key);}
 Console.WriteLine("PASS: published application contains the expanded illustrated sticker catalogue and all referenced PNG resources.");
}

// Every Shift-resize preserves the original aspect and the opposite anchor.
foreach(int corner in Enumerable.Range(0,4))
{
 var result=ShapeGeometry.Resize(original,corner,new PointF(corner is 0 or 3?-30:240,corner is 0 or 1?-20:210),true);
 Near(result.Width/result.Height,original.Width/original.Height);
 if(corner is 0 or 3)Near(result.Right,original.Right);else Near(result.Left,original.Left);
 if(corner is 0 or 1)Near(result.Bottom,original.Bottom);else Near(result.Top,original.Top);
 var tiny=ShapeGeometry.Resize(original,corner,new PointF(corner is 0 or 3?900:-900,corner is 0 or 1?900:-900),true);
 Near(tiny.Width/tiny.Height,1.25f);if(tiny.Width<4||tiny.Height<4)throw new Exception("Aspect-resize minimum violated");
}
var first=new RectangleF(10,20,40,20);var second=new RectangleF(90,60,20,40);
var group=SelectionGeometry.Bounds(new[]{first,second});Near(group.Width,100);Near(group.Height,80);
var destination=new RectangleF(30,40,200,160);var movedFirst=SelectionGeometry.MapBounds(first,group,destination);var movedSecond=SelectionGeometry.MapBounds(second,group,destination);
Near(movedFirst.X,30);Near(movedFirst.Width,80);Near(movedSecond.X,190);Near(movedSecond.Height,80);
var box=new RectangleF(0,0,111,101);if(!box.Contains(first)||!box.Contains(second)||new RectangleF(0,0,100,90).Contains(second))throw new Exception("Marquee containment failure");
var batch=AnnotationTransfer.DeserializeMany(AnnotationTransfer.SerializeMany(new[]{annotation,clone}));if(batch.Count!=2||batch[0].Points[0]!=annotation.Points[0]||batch[1].Bounds!=clone.Bounds)throw new Exception("Group clipboard failure");
Console.WriteLine("PASS: aspect-ratio resize, minimums, group transforms, full marquee containment and group clipboard serialization.");

foreach(var input in new[]{new Size(1920,1080),new Size(3840,2160),new Size(1081,1921),new Size(101,71),new Size(2,2)})
{
 var output=VideoFrames.OutputSize(input);if(output.Width>1920||output.Height>1080||output.Width%2!=0||output.Height%2!=0||output.Width<2||output.Height<2)throw new Exception("Invalid H.264 frame size");
}
var timing=new RecordingClock(100);Near(timing.Elapsed(160),60);timing.SetPaused(true,160);Near(timing.Elapsed(1000),60);timing.SetPaused(true,1100);timing.SetPaused(false,1200);Near(timing.Elapsed(1250),110);timing.SetPaused(true,1300);Near(timing.Elapsed(2000),160);timing.SetPaused(false,2300);Near(timing.Elapsed(2400),260);timing.Reset(3000);Near(timing.Elapsed(3010),10);
foreach(var test in new[]{(R:0,G:0,B:0,Y:16,U:128,V:128),(R:255,G:255,B:255,Y:235,U:128,V:128),(R:255,G:0,B:0,Y:82,U:90,V:240),(R:0,G:0,B:255,Y:41,U:240,V:110)})
{
 var bgra=new byte[16];for(int i=0;i<4;i++){bgra[i*4]=(byte)test.B;bgra[i*4+1]=(byte)test.G;bgra[i*4+2]=(byte)test.R;bgra[i*4+3]=255;}
 var nv12=new byte[6];VideoFrames.ToNv12(bgra,nv12,2,2);if(nv12.Take(4).Any(y=>y!=test.Y)||nv12[4]!=test.U||nv12[5]!=test.V)throw new Exception("NV12 color conversion failed");
}
// 2x2 RGBW must produce neutral chroma; each luma remains in spatial order.
var colored=new byte[]{0,0,255,255,0,255,0,255,255,0,0,255,255,255,255,255};var converted=new byte[6];VideoFrames.ToNv12(colored,converted,2,2);
if(!converted.Take(4).SequenceEqual(new byte[]{82,144,41,235})||converted[4]!=128||converted[5]!=128)throw new Exception("NV12 ordering or chroma averaging failed");
Console.WriteLine("PASS: even H.264 output sizes, repeat pause/resume timing and NV12 conversion including chroma averaging.");

var text=new Annotation{Kind=EditTool.Text,Bounds=new RectangleF(20,30,100,40),Text="Border test"};
foreach(var style in new[]{TextBorderStyle.Solid,TextBorderStyle.Dashed,TextBorderStyle.Dotted,TextBorderStyle.Rounded})
{
 text.SetTextBorder(style,3,Color.DarkBlue);Near(text.Bounds.X,12);Near(text.Bounds.Y,22);Near(text.Bounds.Width,116);Near(text.Bounds.Height,56);
 var copied=AnnotationTransfer.Deserialize(AnnotationTransfer.Serialize(text));if(copied.BorderStyle!=style||copied.BorderWidth!=3||copied.BorderColor.ToArgb()!=Color.DarkBlue.ToArgb())throw new Exception("Border clipboard settings lost");
 var cloneText=text.Clone();if(cloneText.BorderStyle!=style||cloneText.BorderWidth!=3)throw new Exception("Border clone settings lost");
}
text.SetTextBorder(TextBorderStyle.None,2,Color.Red);if(text.Bounds!=new RectangleF(20,30,100,40))throw new Exception("Border padding drifted");
var badge=new Annotation{Kind=EditTool.Counter,Bounds=new RectangleF(10,20,42,42),Text="3"};badge.SetCounterSize(90);Near(badge.Bounds.X+badge.Bounds.Width/2,31);Near(badge.Bounds.Y+badge.Bounds.Height/2,41);Near(badge.Bounds.Width,90);badge.SetCounterSize(999);Near(badge.Bounds.Width,400);badge.SetCounterSize(1);Near(badge.Bounds.Width,12);
Console.WriteLine("PASS: all text-border styles, independent colour/width, stable padding, clipboard/clone preservation and counter sizes/centres/limits.");
for(int seconds=1;seconds<=600;seconds++){long samples=AudioFrames.Samples(seconds*10_000_000L);if(samples!=seconds*44100L||AudioFrames.Ticks(samples)!=seconds*10_000_000L)throw new Exception("Audio timing drift");}
var pcm=new PcmQueue();pcm.Add(new byte[]{1,2,3,4,5,6});if(!pcm.Read(2).SequenceEqual(new byte[]{1,2,3,4}))throw new Exception("PCM sample order failure");pcm.Add(new byte[]{7,8});if(!pcm.Read(3).SequenceEqual(new byte[]{5,6,7,8,0,0}))throw new Exception("PCM buffer stitching or silence padding failure");pcm.Add(new byte[]{9,10});pcm.Clear();if(pcm.Read(1).Any(x=>x!=0))throw new Exception("Muted/paused queue contains stale audio");
Console.WriteLine("PASS: 10-minute PCM clock, ordered audio chunks, underflow silence and queue discard on mute/pause.");

var oldJson=System.Text.Json.Nodes.JsonNode.Parse(AnnotationTransfer.Serialize(text))!.AsObject();oldJson.Remove("BorderStyle");oldJson.Remove("BorderArgb");oldJson.Remove("BorderWidth");var oldCopy=AnnotationTransfer.Deserialize(oldJson.ToJsonString());if(oldCopy.BorderStyle!=TextBorderStyle.None||oldCopy.BorderWidth!=2)throw new Exception("Legacy clipboard defaults not preserved");
Console.WriteLine("PASS: v5 clipboard payloads without border fields retain compatible defaults.");

text.TextBackground=Color.FromArgb(180,255,220,100);
var backgroundCopy=AnnotationTransfer.Deserialize(AnnotationTransfer.Serialize(text));
if(backgroundCopy.TextBackground.ToArgb()!=text.TextBackground.ToArgb()||text.Clone().TextBackground.ToArgb()!=text.TextBackground.ToArgb())throw new Exception("Text background not preserved");
oldJson=System.Text.Json.Nodes.JsonNode.Parse(AnnotationTransfer.Serialize(text))!.AsObject();oldJson.Remove("BackgroundArgb");
if(AnnotationTransfer.Deserialize(oldJson.ToJsonString()).TextBackground.A!=0)throw new Exception("Legacy background must be transparent");
byte[] Pcm(params short[] values){var bytes=new byte[values.Length*2];for(int i=0;i<values.Length;i++)System.Buffers.Binary.BinaryPrimitives.WriteInt16LittleEndian(bytes.AsSpan(i*2,2),values[i]);return bytes;}
var mixed=AudioMix.Mix(Pcm(20000,-20000,1234,0),Pcm(20000,-20000,-1234,7000));
if(!mixed.SequenceEqual(Pcm(short.MaxValue,short.MinValue,0,7000)))throw new Exception("Audio mix clipping, cancellation or pass-through failed");
Console.WriteLine("PASS: text backgrounds survive clipboard/clone, legacy transparency, and microphone/speaker PCM mixing saturates without wraparound.");

// Startup failure cleanup must not hold the callback lock while disposing.
foreach(bool failStart in new[]{false,true})
{
 var device=new FakeAudioCapture(failStart);
 using var source=new SystemAudio(true,()=>false,()=>device);
 if(!device.Started.Wait(TimeSpan.FromSeconds(2)))throw new Exception("Audio owner did not start");
 source.StopCapture();
 if(!device.Disposed.Wait(TimeSpan.FromSeconds(2))||device.CallbackBlocked)throw new Exception("Audio shutdown deadlocked its capture callback");
}
// Repeated stop/dispose with no audio device must be safe and quick.
var mutedSource=new SystemAudio(false,()=>false,()=>throw new Exception("Muted audio opened a device"));
mutedSource.StopCapture();mutedSource.StopCapture();mutedSource.Dispose();mutedSource.Dispose();
Console.WriteLine("PASS: speaker shutdown/startup failure allows capture callbacks during disposal; repeated stop/dispose is safe.");

sealed class FakeAudioCapture : NAudio.Wave.IWaveIn
{
 private readonly bool failStart;
 public readonly ManualResetEventSlim Started=new(),Disposed=new();
 public bool CallbackBlocked;
 public FakeAudioCapture(bool failStart)=>this.failStart=failStart;
 public NAudio.Wave.WaveFormat WaveFormat{get;set;}=new(44100,16,1);
 public event EventHandler<NAudio.Wave.WaveInEventArgs>? DataAvailable;
 public event EventHandler<NAudio.Wave.StoppedEventArgs>? RecordingStopped;
 public void StartRecording(){Started.Set();if(failStart)throw new InvalidOperationException("Simulated device failure");}
 public void StopRecording()=>RecordingStopped?.Invoke(this,new NAudio.Wave.StoppedEventArgs());
 public void Dispose()
 {
  // A device may wait for an in-flight callback before releasing its buffers.
  var callback=Task.Run(()=>DataAvailable?.Invoke(this,new NAudio.Wave.WaveInEventArgs(new byte[128],128)));
  CallbackBlocked=!callback.Wait(TimeSpan.FromSeconds(1));Disposed.Set();
 }
}
