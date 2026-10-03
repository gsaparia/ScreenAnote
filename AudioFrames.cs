namespace ScreenAnote;
internal static class AudioFrames
{
    public const int SampleRate=44100,BytesPerSample=2,ChunkSamples=1024;
    public static long Samples(long ticks)=>Math.Max(0,ticks)/10_000_000*SampleRate+(Math.Max(0,ticks)%10_000_000)*SampleRate/10_000_000;
    public static long Ticks(long samples)=>samples/SampleRate*10_000_000+samples%SampleRate*10_000_000/SampleRate;
}
internal sealed class PcmQueue
{
    private readonly object gate=new();
    private readonly Queue<byte[]> blocks=new();
    private int offset,available;
    public void Clear(){lock(gate){blocks.Clear();offset=available=0;}}
    public void Add(byte[] bytes)
    {
        lock(gate){blocks.Enqueue(bytes);available+=bytes.Length;while(available>AudioFrames.SampleRate*4&&blocks.Count>0){available-=blocks.Dequeue().Length-offset;offset=0;}}
    }
    public byte[] Read(int samples)
    {
        var output=new byte[checked(samples*AudioFrames.BytesPerSample)];
        lock(gate){int written=0;while(written<output.Length&&blocks.Count>0){var block=blocks.Peek();int count=Math.Min(block.Length-offset,output.Length-written);Buffer.BlockCopy(block,offset,output,written,count);offset+=count;written+=count;available-=count;if(offset==block.Length){blocks.Dequeue();offset=0;}}}return output;
    }
}
