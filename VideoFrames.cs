using System.Drawing;
namespace ScreenAnote;
internal static class VideoFrames
{
    public const int Fps=15;
    public const long FrameTicks=10_000_000/Fps;
    public static Size OutputSize(Size region)
    {
        if(region.Width<2||region.Height<2)throw new ArgumentException("Select a region at least 2 × 2 pixels.");
        double scale=Math.Min(1,Math.Min(1920d/region.Width,1080d/region.Height));
        return new Size(Math.Max(2,(int)(region.Width*scale)&~1),Math.Max(2,(int)(region.Height*scale)&~1));
    }
    // BGRA, top row first -> NV12 limited-range BT.601, including 2x2 chroma averaging.
    public static void ToNv12(byte[] rgb,byte[] nv12,int width,int height)
    {
        if(width%2!=0||height%2!=0||rgb.Length!=width*height*4||nv12.Length!=width*height*3/2)throw new ArgumentException("Invalid video frame geometry.");
        static byte Clamp(int n)=>(byte)Math.Clamp(n,0,255);
        int chroma=width*height;
        for(int y=0;y<height;y+=2)for(int x=0;x<width;x+=2)
        {
            int sr=0,sg=0,sb=0;
            for(int dy=0;dy<2;dy++)for(int dx=0;dx<2;dx++)
            {
                int pixel=(y+dy)*width+x+dx,i=pixel*4;int b=rgb[i],g=rgb[i+1],r=rgb[i+2];
                nv12[pixel]=Clamp(((66*r+129*g+25*b+128)>>8)+16);sr+=r;sg+=g;sb+=b;
            }
            int u=chroma+(y/2)*width+x;
            nv12[u]=Clamp(((-38*(sr/4)-74*(sg/4)+112*(sb/4)+128)>>8)+128);
            nv12[u+1]=Clamp(((112*(sr/4)-94*(sg/4)-18*(sb/4)+128)>>8)+128);
        }
    }
}
internal sealed class RecordingClock
{
    public static long Now=>System.Diagnostics.Stopwatch.GetElapsedTime(0).Ticks;
    private readonly object gate=new();
    private long started,pausedAt,pausedTotal;
    private bool paused;
    public RecordingClock(long ticks)=>started=ticks;
    public void Reset(long ticks){lock(gate){started=ticks;pausedAt=pausedTotal=0;paused=false;}}
    public bool Paused{get{lock(gate)return paused;}}
    public void SetPaused(bool value,long ticks){lock(gate){if(paused==value)return;if(value)pausedAt=ticks;else pausedTotal+=ticks-pausedAt;paused=value;}}
    public long Elapsed(long ticks){lock(gate)return Math.Max(0,(paused?pausedAt:ticks)-started-pausedTotal);}
}
