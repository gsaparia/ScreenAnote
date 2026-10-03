using System.Drawing;
namespace ScreenAnote;
internal static class ShapeGeometry
{
    public static RectangleF Box(PointF a,PointF b)=>RectangleF.FromLTRB(Math.Min(a.X,b.X),Math.Min(a.Y,b.Y),Math.Max(a.X,b.X),Math.Max(a.Y,b.Y));
    public static PointF ZoomPan(PointF pointer,PointF anchor,float scale,PointF origin,PointF pan)=>new(pan.X+pointer.X-origin.X-anchor.X*scale,pan.Y+pointer.Y-origin.Y-anchor.Y*scale);
    public static PointF Map(PointF p,RectangleF from,RectangleF to)=>new(to.X+(p.X-from.X)/Math.Max(1,from.Width)*to.Width,to.Y+(p.Y-from.Y)/Math.Max(1,from.Height)*to.Height);
    public static RectangleF Resize(RectangleF original,int handle,PointF point,bool preserveAspect=false)
    {
        float l=original.Left,t=original.Top,r=original.Right,b=original.Bottom;
        if(handle is 0 or 3)l=Math.Min(point.X,r-4);else r=Math.Max(point.X,l+4);
        if(handle is 0 or 1)t=Math.Min(point.Y,b-4);else b=Math.Max(point.Y,t+4);
        if(preserveAspect)
        {
            float ratio=Math.Max(.001f,original.Width)/Math.Max(.001f,original.Height);
            float width=r-l,height=b-t;
            if(width/height>ratio)height=width/ratio;else width=height*ratio;
            if(handle is 0 or 3)l=r-width;else r=l+width;
            if(handle is 0 or 1)t=b-height;else b=t+height;
        }
        return RectangleF.FromLTRB(l,t,r,b);
    }
    public static float Distance(PointF p,PointF a,PointF b)
    {
        float dx=b.X-a.X,dy=b.Y-a.Y,len=dx*dx+dy*dy;
        float v=len==0?0:Math.Clamp(((p.X-a.X)*dx+(p.Y-a.Y)*dy)/len,0,1);
        return MathF.Sqrt(MathF.Pow(p.X-a.X-v*dx,2)+MathF.Pow(p.Y-a.Y-v*dy,2));
    }
}
