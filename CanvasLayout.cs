using System.Drawing;
namespace ScreenAnote;
internal static class CanvasLayout
{
    public static Rectangle Palette(Size area)=>new((area.Width-Math.Min(790,Math.Max(300,area.Width-32)))/2,Math.Max(8,area.Height-(area.Width>=1320?90:174)),Math.Min(790,Math.Max(300,area.Width-32)),86);
    public static Rectangle Zoom(Size area)=>new(16,Math.Max(8,area.Height-76),232,56);
    public static Rectangle Drawer(Size area)
    {int height=Math.Min(388,Math.Max(210,area.Height-210));int y=Math.Max(16,Math.Min((area.Height-height)/2,Palette(area).Top-height-12));return new(Math.Max(8,area.Width-282),y,266,height);}
    public static Rectangle Scale(Rectangle bounds,float factor)=>Rectangle.Round(new RectangleF(bounds.X*factor,bounds.Y*factor,bounds.Width*factor,bounds.Height*factor));
    public static Rectangle Context(Size area,bool drawerVisible,RectangleF? selected)
    {
        int available=area.Width-(drawerVisible?306:32),width=Math.Min(520,Math.Max(250,available));
        int x=selected.HasValue?(int)selected.Value.Left:(available-width)/2+16;
        int y=selected.HasValue?(int)selected.Value.Top-68:16;
        if(y<12 && selected.HasValue)y=(int)selected.Value.Bottom+12;
        var result=new Rectangle(Math.Clamp(x,12,Math.Max(12,available-width+12)),Math.Clamp(y,12,Math.Max(12,area.Height-(area.Width>=1320?160:244))),width,58);
        if(drawerVisible && result.IntersectsWith(Drawer(area)))result.Y=Math.Max(12,Drawer(area).Top-result.Height-8);
        return result;
    }
}
