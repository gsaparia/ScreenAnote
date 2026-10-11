using System.Buffers.Binary;
namespace ScreenAnote;
internal static class AudioMix
{
    public static float Peak(byte[] pcm)
    {
        int peak=0;
        for(int i=0;i+1<pcm.Length;i+=2)peak=Math.Max(peak,Math.Abs((int)BinaryPrimitives.ReadInt16LittleEndian(pcm.AsSpan(i,2))));
        return peak/32768f;
    }
    public static byte[] Mix(byte[] microphone,byte[] speaker)
    {
        // Saturation prevents integer wraparound when both sources are loud.
        for(int i=0;i<microphone.Length;i+=2){int sum=BinaryPrimitives.ReadInt16LittleEndian(microphone.AsSpan(i,2))+BinaryPrimitives.ReadInt16LittleEndian(speaker.AsSpan(i,2));BinaryPrimitives.WriteInt16LittleEndian(microphone.AsSpan(i,2),(short)Math.Clamp(sum,short.MinValue,short.MaxValue));}return microphone;
    }
}

// One instance per recording. A block lookahead peak limiter needs no extra
// capture buffers or deferred work at Stop. Gain attacks immediately and releases
// smoothly over 150 ms; the ceiling leaves a little headroom for AAC encoding.
internal sealed class RecordingAudioMixer
{
    public const double Ceiling=32767*.95;
    private double limiterGain=1;
    private static readonly double Release=1-Math.Exp(-1d/(AudioFrames.SampleRate*.15));
    public byte[] Mix(byte[] microphone,byte[] speaker,int microphonePercent,int speakerPercent)
    {
        if(microphone.Length!=speaker.Length||microphone.Length%2!=0)throw new ArgumentException("Audio blocks must contain matching PCM16 samples.");
        if(microphonePercent<0||microphonePercent>400)throw new ArgumentOutOfRangeException(nameof(microphonePercent));
        if(speakerPercent<0||speakerPercent>100)throw new ArgumentOutOfRangeException(nameof(speakerPercent));
        double mic=microphonePercent/100d,system=speakerPercent/100d,peak=0;
        double Sample(int i)=>BinaryPrimitives.ReadInt16LittleEndian(microphone.AsSpan(i,2))*mic+BinaryPrimitives.ReadInt16LittleEndian(speaker.AsSpan(i,2))*system;
        for(int i=0;i<microphone.Length;i+=2)peak=Math.Max(peak,Math.Abs(Sample(i)));
        double required=peak>Ceiling?Ceiling/peak:1;
        limiterGain=Math.Min(limiterGain,required);
        for(int i=0;i<microphone.Length;i+=2)
        {
            limiterGain=Math.Min(required,limiterGain+(1-limiterGain)*Release);
            double mixed=Sample(i)*limiterGain;
            BinaryPrimitives.WriteInt16LittleEndian(microphone.AsSpan(i,2),(short)Math.Clamp(Math.Round(mixed),-Ceiling,Ceiling));
        }
        return microphone;
    }
}
