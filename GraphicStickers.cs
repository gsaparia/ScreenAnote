using System.Reflection;
using System.Text.Json;
namespace ScreenAnote;
internal sealed class GraphicStickers:IDisposable
{
    private sealed record Entry(string Key,string Name,string Category);
    internal sealed class Sticker:IDisposable
    {
        public string Name{get;}public string Category{get;}
        private Bitmap? image;private readonly string? key;
        public Sticker(string name,string category,Bitmap? image=null,string? key=null){Name=name;Category=category;this.image=image;this.key=key;}
        public Bitmap Image
        {
            get{if(image!=null)return image;using var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("ScreenAnote.Assets.Stickers."+key+".png")??throw new InvalidOperationException("Sticker artwork missing.");using var source=new Bitmap(stream);return image=new Bitmap(source);}
        }
        public void Dispose(){image?.Dispose();image=null;}
    }
    public List<Sticker> Items{get;}=new();
    public GraphicStickers()
    {
        var assembly=Assembly.GetExecutingAssembly();const string prefix="ScreenAnote.Assets.Stickers.";
        using var json=assembly.GetManifestResourceStream(prefix+"catalog.json")??throw new InvalidOperationException("Sticker catalogue missing.");
        foreach(var entry in JsonSerializer.Deserialize<List<Entry>>(json,new JsonSerializerOptions{PropertyNameCaseInsensitive=true})!)
        {Items.Add(new(entry.Name,entry.Category,key:entry.Key));}
    }
    public void Dispose(){foreach(var item in Items)item.Dispose();Items.Clear();}
}
