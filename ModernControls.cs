using System.ComponentModel;
using System.Drawing.Drawing2D;
namespace ScreenAnote;
internal static class Theme
{
    public static readonly Color Accent=Color.FromArgb(45,91,245),Ink=Color.FromArgb(25,35,61),Muted=Color.FromArgb(98,110,131),Line=Color.FromArgb(220,226,237),Surface=Color.FromArgb(242,245,250);
    public static GraphicsPath Round(RectangleF r,float radius){var p=new GraphicsPath();float d=Math.Min(radius*2,Math.Min(r.Width,r.Height));p.AddArc(r.X,r.Y,d,d,180,90);p.AddArc(r.Right-d,r.Y,d,d,270,90);p.AddArc(r.Right-d,r.Bottom-d,d,d,0,90);p.AddArc(r.X,r.Bottom-d,d,d,90,90);p.CloseFigure();return p;}
}
internal sealed class RoundedCard:Panel
{
    public RoundedCard(){DoubleBuffered=true;SetStyle(ControlStyles.AllPaintingInWmPaint|ControlStyles.UserPaint|ControlStyles.OptimizedDoubleBuffer,true);BackColor=Color.White;Padding=new Padding(14);}
    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);if(Width<2||Height<2)return;using var path=Theme.Round(new RectangleF(0,0,Width,Height),13*DeviceDpi/96f);var old=Region;Region=new Region(path);old?.Dispose();
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;
        using var path=Theme.Round(new RectangleF(.5f,.5f,Width-1,Height-1),13*DeviceDpi/96f);using var pen=new Pen(Theme.Line);g.DrawPath(pen,path);
    }
}
internal sealed class ModernButton:Button
{
    private bool hovered;
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]public bool Primary{get;set;}
    private bool selected;
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]public bool Selected{get=>selected;set{if(selected==value)return;selected=value;Invalidate();}}
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]public bool Vertical{get;set;}
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]public bool Outline{get;set;}
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]public bool Underline{get;set;}
    private Image? artwork;
    private Bitmap? thumbnail;
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]public Image? Artwork{get=>artwork;set{if(ReferenceEquals(artwork,value))return;artwork=value;thumbnail?.Dispose();thumbnail=null;Invalidate();}}
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]public string Symbol{get;set;}="";
    public ModernButton(){FlatStyle=FlatStyle.Flat;FlatAppearance.BorderSize=0;BackColor=Color.White;ForeColor=Theme.Ink;Cursor=Cursors.Hand;TabStop=true;SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.SupportsTransparentBackColor,true);}
    protected override void OnPaintBackground(PaintEventArgs e)
    {
        // Floating controls are opaque siblings of the canvas. Never recursively
        // repaint the image through a chain of transparent parent controls.
        var parent=Parent;while(parent!=null && parent.BackColor.A<255)parent=parent.Parent;
        e.Graphics.Clear(parent?.BackColor??Theme.Surface);
    }
    protected override void OnMouseEnter(EventArgs e){hovered=true;Invalidate();base.OnMouseEnter(e);}
    protected override void OnMouseLeave(EventArgs e){hovered=false;Invalidate();base.OnMouseLeave(e);}
    protected override void OnPaint(PaintEventArgs e)
    {
        // Clear again: native Button painting can bypass OnPaintBackground.
        OnPaintBackground(e);
        var g=e.Graphics;float dpi=DeviceDpi/96f;g.SmoothingMode=SmoothingMode.AntiAlias;var fill=Primary?(hovered?Color.FromArgb(27,71,217):Theme.Accent):Selected?Color.FromArgb(225,232,255):hovered?Theme.Surface:BackColor;
        using var path=Theme.Round(new RectangleF(1,1,Width-2,Height-2),9);using var brush=new SolidBrush(fill);g.FillPath(brush,path);
        if(Outline || Artwork!=null){using var border=new Pen(Selected||hovered?Theme.Accent:Theme.Line);g.DrawPath(border,path);}
        if(Underline && Selected){using var bar=new Pen(Theme.Accent,2);g.DrawLine(bar,4,Height-2,Width-4,Height-2);}
        if(Artwork!=null){var bounds=new Size(Math.Max(1,Width-12),Math.Max(1,Height-10));if(thumbnail==null||thumbnail.Size!=bounds){thumbnail?.Dispose();thumbnail=new Bitmap(bounds.Width,bounds.Height);using var tg=Graphics.FromImage(thumbnail);tg.InterpolationMode=InterpolationMode.HighQualityBicubic;tg.DrawImage(Artwork,new Rectangle(Point.Empty,bounds));}g.DrawImageUnscaled(thumbnail,6,5);return;}
        var color=!Enabled?Theme.Muted:Primary||(!Selected&&fill.GetBrightness()<.42f)?Color.White:Selected?Theme.Accent:Theme.Ink;
        if(Symbol.Length>0){var icon=new RectangleF(Vertical || Text.Length==0?(Width-24*dpi)/2:10*dpi,Vertical && Text.Length>0?9*dpi:(Height-24*dpi)/2,24*dpi,24*dpi);DrawIcon(g,icon,Symbol,color);}
        var text=Vertical?new Rectangle(0,Height-(int)(24*dpi),Width,(int)(21*dpi)):new Rectangle(Symbol.Length>0?(int)(40*dpi):5,0,Width-(Symbol.Length>0?(int)(45*dpi):10),Height);
        if(Text.Length>0)TextRenderer.DrawText(g,Text,Font,text,color,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);
        if(Focused){using var pen=new Pen(Theme.Accent){DashStyle=DashStyle.Dot};g.DrawRectangle(pen,3,3,Width-7,Height-7);}
    }
    private static void DrawIcon(Graphics g,RectangleF r,string symbol,Color color)
    {
        var state=g.Save();g.TranslateTransform(r.X,r.Y);g.ScaleTransform(r.Width/24,r.Height/24);using var pen=new Pen(color,1.8f){StartCap=LineCap.Round,EndCap=LineCap.Round,LineJoin=LineJoin.Round};using var brush=new SolidBrush(color);
        switch(symbol)
        {
            case "mic":case "mic-off":g.DrawArc(pen,8,2,8,13,180,180);g.DrawArc(pen,8,2,8,13,0,180);g.DrawArc(pen,5,6,14,13,0,180);g.DrawLine(pen,12,19,12,23);g.DrawLine(pen,8,23,16,23);if(symbol=="mic-off")g.DrawLine(pen,3,3,21,21);break;
            case "video":g.DrawRectangle(pen,2,6,13,13);g.DrawPolygon(pen,[new PointF(15,10),new(22,6),new(22,19),new(15,15)]);break;
            case "select":g.DrawPolygon(pen,[new PointF(5,3),new(5,20),new(10,15),new(14,22),new(17,20),new(13,13),new(21,13)]);break;
            case "pen":g.DrawLines(pen,[new PointF(4,20),new(7,12),new(17,3),new(22,8),new(12,18),new(4,20)]);g.DrawLine(pen,15,5,20,10);break;
            case "shape":g.DrawRectangle(pen,4,4,16,16);break;
            case "arrow":g.DrawLine(pen,4,20,20,4);g.DrawLines(pen,[new PointF(10,4),new(20,4),new(20,14)]);break;
            case "text":g.DrawLine(pen,4,5,20,5);g.DrawLine(pen,12,5,12,21);g.DrawLine(pen,8,21,16,21);break;
            case "highlight":g.DrawPolygon(pen,[new PointF(5,15),new(15,4),new(21,10),new(11,21)]);g.DrawLine(pen,3,22,14,22);break;
            case "redact":g.FillRectangle(brush,3,5,18,5);g.FillRectangle(brush,3,15,18,5);break;
            case "sticker":g.DrawEllipse(pen,2,2,20,20);g.FillEllipse(brush,7,8,2,2);g.FillEllipse(brush,15,8,2,2);g.DrawArc(pen,7,9,10,9,15,150);break;
            case "counter":g.DrawEllipse(pen,2,2,20,20);using(var font=new Font("Segoe UI",11,FontStyle.Bold))g.DrawString("1",font,brush,6,3);break;
            case "heart":g.DrawBezier(pen,new PointF(12,20),new(0,13),new(1,1),new(12,7));g.DrawBezier(pen,new PointF(12,7),new(23,1),new(24,13),new(12,20));break;
            case "star":g.DrawPolygon(pen,[new PointF(12,2),new(15,8),new(22,9),new(17,14),new(18,21),new(12,18),new(6,21),new(7,14),new(2,9),new(9,8)]);break;
            case "flag":g.DrawLine(pen,5,3,5,22);g.DrawLines(pen,[new PointF(5,4),new(20,4),new(18,14),new(5,14)]);break;
            case "save":g.DrawRectangle(pen,4,3,16,18);g.DrawRectangle(pen,8,3,9,6);g.DrawRectangle(pen,8,14,9,7);break;
            case "copy":g.DrawRectangle(pen,8,7,13,15);g.DrawLines(pen,[new PointF(16,4),new(3,4),new(3,18)]);break;
            case "capture":g.DrawRectangle(pen,2,7,20,14);g.DrawEllipse(pen,8,10,8,8);g.DrawLines(pen,[new PointF(7,7),new(9,3),new(15,3),new(17,7)]);break;
            case "undo":g.DrawArc(pen,5,6,15,15,230,240);g.DrawLines(pen,[new PointF(9,3),new(3,8),new(10,11)]);break;
            case "redo":g.DrawArc(pen,3,6,15,15,70,240);g.DrawLines(pen,[new PointF(15,3),new(21,8),new(14,11)]);break;
            case "fit":g.DrawLines(pen,[new PointF(8,3),new(3,3),new(3,8)]);g.DrawLines(pen,[new PointF(16,3),new(21,3),new(21,8)]);g.DrawLines(pen,[new PointF(3,16),new(3,21),new(8,21)]);g.DrawLines(pen,[new PointF(16,21),new(21,21),new(21,16)]);break;
            case "delete":g.DrawRectangle(pen,6,7,12,14);g.DrawLine(pen,3,6,21,6);g.DrawLine(pen,9,3,15,3);g.DrawLine(pen,10,10,10,18);g.DrawLine(pen,14,10,14,18);break;
            default:using(var font=new Font("Segoe UI Symbol",15))using(var format=new StringFormat{Alignment=StringAlignment.Center,LineAlignment=StringAlignment.Center})g.DrawString(symbol,font,brush,new RectangleF(0,0,24,24),format);break;
        }
        g.Restore(state);
    }
    protected override void Dispose(bool disposing){if(disposing)thumbnail?.Dispose();base.Dispose(disposing);}
}
