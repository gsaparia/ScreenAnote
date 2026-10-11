using System.Buffers.Binary;
using System.Text;
namespace ScreenAnote;
internal static class Mp4FragmentInspector
{
    public static int CompleteFragments(Stream input)
    {
        long limit=input.Length;int fragments=0;bool pending=false,initialization=false;
        Span<byte> header=stackalloc byte[16];
        while(input.Position+8<=limit)
        {
            long start=input.Position;input.ReadExactly(header[..8]);
            ulong size=BinaryPrimitives.ReadUInt32BigEndian(header[..4]);
            string type=Encoding.ASCII.GetString(header.Slice(4,4));int headerLength=8;
            if(size==1){if(input.Position+8>limit)break;input.ReadExactly(header.Slice(8,8));size=BinaryPrimitives.ReadUInt64BigEndian(header.Slice(8,8));headerLength=16;}
            // A zero-size box extends to EOF, which is not a committed boundary
            // while the recording is still growing.
            if(size==0||size<(ulong)headerLength||size>(ulong)(limit-start))break;
            if(type=="moov")initialization=true;
            if(type=="moof")pending=initialization;
            if(type=="mdat"&&pending){fragments++;pending=false;}
            input.Position=start+(long)size;
        }
        return fragments;
    }
    public static int CompleteFragments(string path)
    {
        using var file=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete);
        return CompleteFragments(file);
    }
}
