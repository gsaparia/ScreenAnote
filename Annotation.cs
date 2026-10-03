using System.Drawing;
using System.Drawing.Drawing2D;
namespace ScreenAnote;
internal enum TextBorderStyle{None,Solid,Dashed,Dotted,Rounded}
internal sealed class Annotation
{
    public EditTool Kind;
    public RectangleF Bounds;
    public Color Color=Color.Red;
    public float Stroke=4,FontSize=24;
    public string Text="";
    public List<PointF> Points=new();
    public Bitmap? Asset;
    public bool Tint;
    public TextBorderStyle BorderStyle;
    public Color BorderColor=Color.RoyalBlue;
    public float BorderWidth=2;
    public float TextPadding=>Kind==EditTool.Text&&BorderStyle!=TextBorderStyle.None?Math.Max(8,BorderWidth+4):0;
    public Annotation Clone()=>new(){Kind=Kind,Bounds=Bounds,Color=Color,Stroke=Stroke,FontSize=FontSize,Text=Text,Points=new(Points),Asset=Asset,Tint=Tint,BorderStyle=BorderStyle,BorderColor=BorderColor,BorderWidth=BorderWidth};
    public void SetTextBorder(TextBorderStyle style,float width,Color color)
    {
        if(Kind!=EditTool.Text)return;
        width=Math.Clamp(width,1,20);float delta=(style==TextBorderStyle.None?0:Math.Max(8,width+4))-TextPadding;
        Bounds=new RectangleF(Bounds.X-delta,Bounds.Y-delta,Math.Max(8,Bounds.Width+delta*2),Math.Max(8,Bounds.Height+delta*2));
        BorderStyle=style;BorderWidth=Math.Clamp(width,1,20);BorderColor=color;
    }
    public void SetCounterSize(float diameter)
    {
        if(Kind!=EditTool.Counter)return;diameter=Math.Clamp(diameter,12,400);
        Bounds=new RectangleF(Bounds.X+(Bounds.Width-diameter)/2,Bounds.Y+(Bounds.Height-diameter)/2,diameter,diameter);
    }
    public void Transform(RectangleF target,bool scaleFont=false)
    {
        var old=Bounds;Points=Points.Select(p=>ShapeGeometry.Map(p,old,target)).ToList();
        if(scaleFont && Kind is EditTool.Text or EditTool.Sticker)FontSize=Math.Clamp(FontSize*target.Height/Math.Max(1,old.Height),6,1000);
        Bounds=target;
    }
    public bool Hit(PointF p,float tolerance)
    {
        if(Kind is EditTool.Pen or EditTool.Arrow){for(int i=1;i<Points.Count;i++)if(ShapeGeometry.Distance(p,Points[i-1],Points[i])<=tolerance+Stroke/2)return true;return Points.Count==1 && ShapeGeometry.Distance(p,Points[0],Points[0])<=tolerance+Stroke/2;}
        var b=Bounds;b.Inflate(tolerance,tolerance);return b.Contains(p);
    }
    public void Draw(Graphics g)
    {
        using var pen=new Pen(Color,Stroke){StartCap=LineCap.Round,EndCap=LineCap.Round};
        switch(Kind)
        {
            case EditTool.Pen:if(Points.Count>1)g.DrawLines(pen,Points.ToArray());else if(Points.Count==1){using var dot=new SolidBrush(Color);g.FillEllipse(dot,Points[0].X-Stroke/2,Points[0].Y-Stroke/2,Stroke,Stroke);}break;
            case EditTool.Arrow:if(Points.Count>=2){using var cap=new AdjustableArrowCap(4,5,true);pen.CustomEndCap=cap;g.DrawLine(pen,Points[0],Points[^1]);}break;
            case EditTool.Rectangle:g.DrawRectangle(pen,Bounds.X,Bounds.Y,Bounds.Width,Bounds.Height);break;
            case EditTool.Ellipse:g.DrawEllipse(pen,Bounds);break;
            case EditTool.Highlight:using(var brush=new SolidBrush(System.Drawing.Color.FromArgb(85,Color)))g.FillRectangle(brush,Bounds);break;
            case EditTool.Redact:using(var brush=new SolidBrush(Color))g.FillRectangle(brush,Bounds);break;
            case EditTool.Counter:
                using(var fill=new SolidBrush(Color))g.FillEllipse(fill,Bounds);
                using(var outline=new Pen(System.Drawing.Color.White,Math.Max(2,Bounds.Width/18)))g.DrawEllipse(outline,Bounds);
                using(var font=new Font("Segoe UI",Math.Max(6,Math.Min(Bounds.Width,Bounds.Height)*.48f),FontStyle.Bold,GraphicsUnit.Pixel))
                using(var format=new StringFormat{Alignment=StringAlignment.Center,LineAlignment=StringAlignment.Center})g.DrawString(Text,font,Brushes.White,Bounds,format);break;
            case EditTool.Text:case EditTool.Sticker:
                var textBox=Bounds;
                if(Kind==EditTool.Text && BorderStyle!=TextBorderStyle.None)
                {
                    using var border=new Pen(BorderColor,BorderWidth){DashStyle=BorderStyle==TextBorderStyle.Dashed?DashStyle.Dash:BorderStyle==TextBorderStyle.Dotted?DashStyle.Dot:DashStyle.Solid};
                    var box=Bounds;box.Inflate(-BorderWidth/2,-BorderWidth/2);
                    if(box.Width>0&&box.Height>0)
                    {
                        if(BorderStyle==TextBorderStyle.Rounded){using var path=new GraphicsPath();float d=Math.Min(16,Math.Min(box.Width,box.Height));path.AddArc(box.Left,box.Top,d,d,180,90);path.AddArc(box.Right-d,box.Top,d,d,270,90);path.AddArc(box.Right-d,box.Bottom-d,d,d,0,90);path.AddArc(box.Left,box.Bottom-d,d,d,90,90);path.CloseFigure();g.DrawPath(border,path);}
                        else g.DrawRectangle(border,box.X,box.Y,box.Width,box.Height);
                    }
                    textBox.Inflate(-TextPadding,-TextPadding);
                }
                using(var font=new Font(Kind==EditTool.Sticker?"Segoe UI Emoji":"Segoe UI",FontSize,FontStyle.Bold,GraphicsUnit.Pixel))
                using(var brush=new SolidBrush(Color))if(textBox.Width>0&&textBox.Height>0)g.DrawString(Text,font,brush,textBox,StringFormat.GenericTypographic);break;
            case EditTool.ImageSticker:
                if(Asset==null)break;
                if(!Tint)g.DrawImage(Asset,Bounds);
                else{
                    using var attributes=new System.Drawing.Imaging.ImageAttributes();
                    var matrix=new System.Drawing.Imaging.ColorMatrix(new[]{new float[]{0,0,0,0,0},new float[]{0,0,0,0,0},new float[]{0,0,0,0,0},new float[]{0,0,0,1,0},new float[]{Color.R/255f,Color.G/255f,Color.B/255f,0,1}});
                    attributes.SetColorMatrix(matrix);g.DrawImage(Asset,Rectangle.Round(Bounds),0,0,Asset.Width,Asset.Height,GraphicsUnit.Pixel,attributes);
                }break;
        }
    }
}
