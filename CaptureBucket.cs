using System.Text.Json;
using System.IO.Compression;
using System.Buffers.Binary;
namespace ScreenAnote;
internal sealed record BucketEntry(Guid Id,string Name,DateTime Created,DateTime Updated,string Revision);
internal sealed record BucketWork(string[] Annotations,int NextCounter);
internal sealed record BucketArchive(string Format,int Version,BucketEntry[] Items);
// Publish metadata only after the entire new revision has been written. Failed
// saves leave the previous revision intact; full images are never held by the list.
internal sealed class CaptureBucket
{
    private readonly string root;
    public int UnreadableItems{get;private set;}
    public CaptureBucket(string? directory=null)=>root=directory??Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"ScreenAnote","CaptureBucket");
    private string Folder(Guid id)=>Path.Combine(root,id.ToString("N"));
    private string Trash=>Path.Combine(root,".deleted");
    public IReadOnlyList<BucketEntry> List()
    {
        UnreadableItems=0;if(!Directory.Exists(root))return [];
        var entries=new List<BucketEntry>();
        foreach(var path in Directory.EnumerateDirectories(root))if(Guid.TryParseExact(Path.GetFileName(path),"N",out var id))
        {
            try{entries.Add(Read(id));}catch(Exception ex) when(ex is IOException or UnauthorizedAccessException or JsonException or InvalidDataException){UnreadableItems++;}
        }
        return entries.OrderByDescending(e=>e.Updated).ToList();
    }
    public BucketEntry Read(Guid id)
    {
        var item=JsonSerializer.Deserialize<BucketEntry>(File.ReadAllText(Path.Combine(Folder(id),"entry.json")))??throw new InvalidDataException("Invalid bucket item.");
        if(item.Id!=id||!Guid.TryParseExact(item.Revision,"N",out _)||string.IsNullOrWhiteSpace(item.Name))throw new InvalidDataException("Invalid bucket metadata.");return item;
    }
    public string DataFolder(BucketEntry entry){var current=Read(entry.Id);return Path.Combine(Folder(current.Id),current.Revision);}
    public BucketEntry Save(string name,Action<string> writeFiles,Guid? existing=null)
    {
        name=name.Trim();if(name.Length==0||name.Length>100)throw new ArgumentException("Use a bucket name from 1 to 100 characters.");
        var old=existing.HasValue?Read(existing.Value):null;var id=old?.Id??Guid.NewGuid();
        var revision=Guid.NewGuid().ToString("N");var folder=Folder(id);Directory.CreateDirectory(folder);
        var data=Path.Combine(folder,revision);Directory.CreateDirectory(data);bool committed=false;
        var temp=Path.Combine(folder,"entry-"+revision+".tmp");
        try
        {
            writeFiles(data);
            var now=DateTime.UtcNow;var entry=new BucketEntry(id,name,old?.Created??now,now,revision);
            File.WriteAllText(temp,JsonSerializer.Serialize(entry));File.Move(temp,Path.Combine(folder,"entry.json"),true);committed=true;
            if(old!=null)Cleanup(Path.Combine(folder,old.Revision));return entry;
        }
        finally{if(!committed){Cleanup(data);try{File.Delete(temp);}catch(IOException){}if(old==null)Cleanup(folder);}}
    }
    public void Rename(Guid id,string name)
    {
        name=name.Trim();if(name.Length==0||name.Length>100)throw new ArgumentException("Use a bucket name from 1 to 100 characters.");
        var entry=Read(id) with{Name=name};var temp=Path.Combine(Folder(id),"rename-"+Guid.NewGuid().ToString("N")+".tmp");
        try{File.WriteAllText(temp,JsonSerializer.Serialize(entry));File.Move(temp,Path.Combine(Folder(id),"entry.json"),true);}finally{if(File.Exists(temp))File.Delete(temp);}
    }
    public void Delete(Guid id){Read(id);Directory.CreateDirectory(Trash);var target=Path.Combine(Trash,id.ToString("N"));Directory.Move(Folder(id),target);Directory.SetLastWriteTimeUtc(target,DateTime.UtcNow);}
    public bool HasDeleted=>Directory.Exists(Trash)&&Directory.EnumerateDirectories(Trash).Any(p=>Guid.TryParseExact(Path.GetFileName(p),"N",out _));
    public Guid? UndoDelete()
    {
        if(!Directory.Exists(Trash))return null;
        var path=Directory.EnumerateDirectories(Trash).Where(p=>Guid.TryParseExact(Path.GetFileName(p),"N",out _)).OrderByDescending(Directory.GetLastWriteTimeUtc).FirstOrDefault();
        if(path==null)return null;var id=Guid.ParseExact(Path.GetFileName(path),"N");Directory.Move(path,Folder(id));return id;
    }
    public void Export(string file)
    {
        var items=List().ToArray();if(UnreadableItems>0)throw new InvalidDataException("Some bucket items are unreadable. Restore or remove them before saving the entire bucket.");
        if(items.Length>2000)throw new InvalidDataException("Bucket archive limit is 2000 items.");
        long total=0;foreach(var item in items)foreach(var name in new[]{"original.png","work.json","thumbnail.png"})total+=new FileInfo(Path.Combine(DataFolder(item),name)).Length;
        if(total>8L*1024*1024*1024)throw new InvalidDataException("Bucket archive exceeds 8 GB expanded.");
        var temporary=file+"."+Guid.NewGuid().ToString("N")+".tmp";
        try
        {
            using(var zip=ZipFile.Open(temporary,ZipArchiveMode.Create))
            {
                using(var writer=new StreamWriter(zip.CreateEntry("manifest.json").Open()))writer.Write(JsonSerializer.Serialize(new BucketArchive("ScreenAnote.bucket",1,items)));
                foreach(var item in items)foreach(var name in new[]{"original.png","work.json","thumbnail.png"})zip.CreateEntryFromFile(Path.Combine(DataFolder(item),name),item.Id.ToString("N")+"/"+name,CompressionLevel.Fastest);
            }
            File.Move(temporary,file,true);
        }
        finally{if(File.Exists(temporary))File.Delete(temporary);}
    }
    // Import adds independent copies. No current item is replaced, even when
    // opening a backup made on this computer. Stage everything before publishing.
    public int Import(string file)
    {
        using var zip=ZipFile.OpenRead(file);
        if(zip.Entries.Count>6001||zip.Entries.Sum(e=>e.Length)>8L*1024*1024*1024)throw new InvalidDataException("Bucket archive is too large (2000 items / 8 GB expanded).");
        if(zip.Entries.Select(e=>e.FullName).Distinct(StringComparer.Ordinal).Count()!=zip.Entries.Count)throw new InvalidDataException("Bucket archive has duplicate entries.");
        var manifest=zip.GetEntry("manifest.json")??throw new InvalidDataException("This is not a ScreenAnote bucket archive.");
        if(manifest.Length>2_000_000)throw new InvalidDataException("Bucket manifest is too large.");
        BucketArchive archive;using(var reader=new StreamReader(manifest.Open()))archive=JsonSerializer.Deserialize<BucketArchive>(reader.ReadToEnd())??throw new InvalidDataException("Invalid bucket manifest.");
        if(archive.Format!="ScreenAnote.bucket"||archive.Version!=1||archive.Items==null||archive.Items.Length>2000||archive.Items.Select(e=>e.Id).Distinct().Count()!=archive.Items.Length)throw new InvalidDataException("Unsupported bucket archive.");
        var allowed=new HashSet<string>(StringComparer.Ordinal){"manifest.json"};
        foreach(var item in archive.Items)
        {
            if(string.IsNullOrWhiteSpace(item.Name)||item.Name.Length>100)throw new InvalidDataException("Invalid bucket item name.");
            foreach(var name in new[]{"original.png","work.json","thumbnail.png"})allowed.Add(item.Id.ToString("N")+"/"+name);
        }
        if(zip.Entries.Count!=allowed.Count||zip.Entries.Any(e=>!allowed.Contains(e.FullName)))throw new InvalidDataException("Bucket archive has missing or unexpected files.");
        Directory.CreateDirectory(root);var staging=Path.Combine(root,".import-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(staging);
        var prepared=new List<Guid>();var published=new List<Guid>();bool committed=false;
        try
        {
            foreach(var item in archive.Items)
            {
                var id=Guid.NewGuid();var revision=Guid.NewGuid().ToString("N");var folder=Path.Combine(staging,id.ToString("N"));var data=Path.Combine(folder,revision);Directory.CreateDirectory(data);
                foreach(var name in new[]{"original.png","work.json","thumbnail.png"})
                {
                    var entry=zip.GetEntry(item.Id.ToString("N")+"/"+name)!;
                    if(entry.Length>(name=="work.json"?32_000_000:name=="thumbnail.png"?4_000_000:450_000_000))throw new InvalidDataException("Bucket item file exceeds its size limit.");
                    using var input=entry.Open();using var output=File.Create(Path.Combine(data,name));input.CopyTo(output);
                }
                ValidatePng(Path.Combine(data,"original.png"),100_000_000);ValidatePng(Path.Combine(data,"thumbnail.png"),1_000_000);
                var work=JsonSerializer.Deserialize<BucketWork>(File.ReadAllText(Path.Combine(data,"work.json")))??throw new InvalidDataException("Invalid bucket annotations.");
                if(work.NextCounter<1||work.Annotations==null||work.Annotations.Length>1000||work.Annotations.Any(a=>a==null||a.Length>24_000_000))throw new InvalidDataException("Invalid bucket work data.");
                foreach(var annotation in work.Annotations){using var parsed=JsonDocument.Parse(annotation);if(parsed.RootElement.ValueKind!=JsonValueKind.Object)throw new InvalidDataException("Invalid bucket annotation.");}
                File.WriteAllText(Path.Combine(folder,"entry.json"),JsonSerializer.Serialize(item with{Id=id,Revision=revision}));prepared.Add(id);
            }
            foreach(var id in prepared){Directory.Move(Path.Combine(staging,id.ToString("N")),Folder(id));published.Add(id);}
            committed=true;return prepared.Count;
        }
        finally{if(!committed)foreach(var id in published)Cleanup(Folder(id));Cleanup(staging);}
    }
    private static void ValidatePng(string file,long maxPixels)
    {
        using var stream=File.OpenRead(file);Span<byte> header=stackalloc byte[24];stream.ReadExactly(header);
        if(!header[..8].SequenceEqual(new byte[]{137,80,78,71,13,10,26,10})||!header.Slice(12,4).SequenceEqual("IHDR"u8))throw new InvalidDataException("Invalid bucket PNG.");
        uint width=BinaryPrimitives.ReadUInt32BigEndian(header.Slice(16,4)),height=BinaryPrimitives.ReadUInt32BigEndian(header.Slice(20,4));
        if(width==0||height==0||width>100_000||height>100_000||(long)width*height>maxPixels)throw new InvalidDataException("Bucket image dimensions exceed the limit.");
    }
    private static void Cleanup(string path){try{Directory.Delete(path,true);}catch(IOException){}catch(UnauthorizedAccessException){}}
}
