using System.Drawing;
using System.Drawing.Imaging;
using System.Text.Json;
namespace ScreenAnote;
internal static class AnnotationTransfer
{
    public const string Format="ScreenAnote.Annotation.v1";
    public const string GroupFormat="ScreenAnote.Annotations.v2";
    public static string SerializeMany(IEnumerable<Annotation> items)
    {
        var payload=JsonSerializer.Serialize(items.Select(Serialize).ToArray());
        if(payload.Length>32_000_000)throw new InvalidOperationException("Selected annotations exceed the clipboard size limit.");return payload;
    }
    public static List<Annotation> DeserializeMany(string json)
    {
        if(json.Length>32_000_000)throw new InvalidOperationException("Annotation clipboard payload is too large.");
        var payload=JsonSerializer.Deserialize<string[]>(json);
        if(payload==null||payload.Length==0||payload.Length>1000)throw new InvalidOperationException("Invalid annotation collection.");
        var result=new List<Annotation>();
        try{foreach(var item in payload)result.Add(Deserialize(item));return result;}
        catch{foreach(var item in result)item.Asset?.Dispose();throw;}
    }
    private sealed record Payload(EditTool Kind,float X,float Y,float W,float H,int Argb,float Stroke,float Font,string Text,float[][] Points,string? Png,bool Tint,TextBorderStyle BorderStyle=TextBorderStyle.None,int BorderArgb=unchecked((int)0xff4169e1),float BorderWidth=2,int BackgroundArgb=0);
    public static string Serialize(Annotation item)
    {
        string? png=null;if(item.Asset!=null){using var stream=new MemoryStream();item.Asset.Save(stream,ImageFormat.Png);png=Convert.ToBase64String(stream.ToArray());}
        return JsonSerializer.Serialize(new Payload(item.Kind,item.Bounds.X,item.Bounds.Y,item.Bounds.Width,item.Bounds.Height,item.Color.ToArgb(),item.Stroke,item.FontSize,item.Text,item.Points.Select(p=>new[]{p.X,p.Y}).ToArray(),png,item.Tint,item.BorderStyle,item.BorderColor.ToArgb(),item.BorderWidth,item.TextBackground.ToArgb()));
    }
    public static Annotation Deserialize(string json)
    {
        if(json.Length>24_000_000)throw new InvalidOperationException("Annotation clipboard payload is too large.");
        var p=JsonSerializer.Deserialize<Payload>(json)??throw new InvalidOperationException("Invalid annotation data.");
        if(!Enum.IsDefined(p.BorderStyle)||!float.IsFinite(p.BorderWidth)||p.BorderWidth<1||p.BorderWidth>20)throw new InvalidOperationException("Invalid text border data.");
        bool Valid(float n)=>float.IsFinite(n)&&Math.Abs(n)<=1_000_000;
        if(!Enum.IsDefined(p.Kind)||p.Kind is EditTool.Select or EditTool.Pan or EditTool.Crop||!Valid(p.X)||!Valid(p.Y)||!Valid(p.W)||!Valid(p.H)||p.W<0||p.H<0||!Valid(p.Stroke)||p.Stroke<1||p.Stroke>1000||!Valid(p.Font)||p.Font<6||p.Font>1000||p.Points==null||p.Points.Length>100_000||p.Points.Any(a=>a==null||a.Length!=2||!Valid(a[0])||!Valid(a[1]))||p.Text==null||p.Text.Length>100_000)throw new InvalidOperationException("Invalid annotation geometry.");
        var item=new Annotation{Kind=p.Kind,Bounds=new(p.X,p.Y,p.W,p.H),Color=Color.FromArgb(p.Argb),Stroke=p.Stroke,FontSize=p.Font,Text=p.Text,Points=p.Points.Select(a=>new PointF(a[0],a[1])).ToList(),Tint=p.Tint,BorderStyle=p.BorderStyle,BorderColor=Color.FromArgb(p.BorderArgb),BorderWidth=p.BorderWidth,TextBackground=Color.FromArgb(p.BackgroundArgb)};
        if(p.Png!=null){using var stream=new MemoryStream(Convert.FromBase64String(p.Png));using var image=System.Drawing.Image.FromStream(stream);if((long)image.Width*image.Height>4_000_000)throw new InvalidOperationException("Pasted sticker exceeds 4 megapixels.");item.Asset=new Bitmap(image);}
        return item;
    }
}
