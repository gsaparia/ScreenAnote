using System.Buffers.Binary;
namespace ScreenAnote;
internal static class AudioMix
{
    public static byte[] Mix(byte[] microphone,byte[] speaker)
    {
        // Saturation prevents integer wraparound when both sources are loud.
        for(int i=0;i<microphone.Length;i+=2){int sum=BinaryPrimitives.ReadInt16LittleEndian(microphone.AsSpan(i,2))+BinaryPrimitives.ReadInt16LittleEndian(speaker.AsSpan(i,2));BinaryPrimitives.WriteInt16LittleEndian(microphone.AsSpan(i,2),(short)Math.Clamp(sum,short.MinValue,short.MaxValue));}return microphone;
    }
}
