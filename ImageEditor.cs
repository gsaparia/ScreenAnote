using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
namespace ScreenAnote;
internal sealed class ImageEditor:Control
{
    private Bitmap? background,preview;
    private bool previewDirty=true;
    private List<Annotation> annotations=new();
    private readonly List<Bitmap> assets=new();
    private readonly LinkedList<State> undo=new();
    private readonly Stack<State> redo=new();
    private readonly List<Annotation> selection=new();
    private Annotation? selected{get=>selection.LastOrDefault();set{selection.Clear();if(value!=null)selection.Add(value);}}
    private Annotation? draft;
    private List<Annotation> originals=new();
    private RectangleF originalGroup,marquee;
    private bool selecting,togglePending;
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] public bool GestureActive=>moving||resizing||panning||drawing||selecting;
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] public int SelectionCount=>selection.Count;
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] public bool SelectionHasText=>selection.Any(a=>a.Kind is EditTool.Text or EditTool.Sticker);
    private RectangleF SelectionBounds=>SelectionGeometry.Bounds(selection.Select(a=>a.Bounds));
    private EditTool tool=EditTool.Select;
    private Color ink=Color.Red;
    private int stroke=4,fontSize=24,counterSize=42;
    private TextBorderStyle borderStyle;private Color borderColor=Color.RoyalBlue;private float borderWidth=2;
    private Color textBackground=Color.Transparent;
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] public Color DefaultTextBackground=>textBackground;
    public void SetTextBackground(Color color)
    {
        textBackground=color;if(selection.Any(a=>a.Kind==EditTool.Text)){Snapshot();foreach(var a in selection.Where(a=>a.Kind==EditTool.Text))a.TextBackground=color;Finish();SelectionChanged?.Invoke(this,EventArgs.Empty);}
    }
    private Bitmap? import;
    private PointF start;
    private Point lastScreen;
    private bool drawing,moving,panning,resizing,gestureSaved;
    private int handle=-1;
    private bool spaceHeld,duplicatePending;
    private readonly CounterSequence counter=new();
    private int pasteCount;
    private float zoom=1;
    private PointF pan;
    private const long Budget=160L*1024*1024;
    private sealed record State(Bitmap Background,List<Annotation> Annotations,int[] Selected,int Counter):IDisposable{public void Dispose()=>Background.Dispose();}
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] public Bitmap? Image=>background;
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] public int NextCounter=>counter.Next;
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] public bool SelectionHasCounter=>selection.Any(a=>a.Kind==EditTool.Counter);
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] public int CounterSize{get=>counterSize;set{counterSize=Math.Clamp(value,12,400);if(SelectionHasCounter){Snapshot();foreach(var a in selection)a.SetCounterSize(counterSize);Finish();SelectionChanged?.Invoke(this,EventArgs.Empty);}}}
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] public TextBorderStyle DefaultBorder=>borderStyle;
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] public Color DefaultBorderColor=>borderColor;
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] public float DefaultBorderWidth=>borderWidth;
    public void SetTextBorder(TextBorderStyle style,float width,Color color)
    {
        borderStyle=style;borderWidth=width;borderColor=color;
        if(selection.Any(a=>a.Kind==EditTool.Text)){Snapshot();foreach(var a in selection)a.SetTextBorder(style,width,color);Finish();SelectionChanged?.Invoke(this,EventArgs.Empty);}
    }
    public void ResetCounter(int next=1){Snapshot();counter.Reset(next);Finish(false);}
    public void SetSpacePan(bool held){spaceHeld=held;if(!held && panning){panning=false;Capture=false;}Cursor=held?Cursors.Hand:Tool==EditTool.Select?Cursors.Default:Tool==EditTool.Pan?Cursors.Hand:Cursors.Cross;}
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] public EditTool Tool{get=>tool;set{tool=value;Cursor=spaceHeld || value==EditTool.Pan?Cursors.Hand:value==EditTool.Select?Cursors.Default:Cursors.Cross;}}
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] public Color Ink{get=>ink;set{ink=value;if(selection.Count>0){Snapshot();foreach(var a in selection){a.Color=value;if(a.Kind==EditTool.ImageSticker)a.Tint=true;}Finish();}}}
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] public int Stroke{get=>stroke;set{stroke=value;if(selection.Count>0){Snapshot();foreach(var a in selection)a.Stroke=value;Finish();}}}
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] public int FontSize{get=>fontSize;set{fontSize=value;if(selection.Any(a=>a.Kind is EditTool.Text or EditTool.Sticker)){Snapshot();foreach(var a in selection.Where(a=>a.Kind is EditTool.Text or EditTool.Sticker)){float ratio=value/a.FontSize;a.Bounds=new RectangleF(a.Bounds.Location,new SizeF(Math.Max(8,(a.Bounds.Width-2*a.TextPadding)*ratio+2*a.TextPadding),Math.Max(8,(a.Bounds.Height-2*a.TextPadding)*ratio+2*a.TextPadding)));a.FontSize=value;}Finish();}}}
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] public string Sticker{get;set;}="☺";
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] public Func<string?,string?>? RequestText{get;set;}
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] public float ZoomPercent=>DisplayScale*100;
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] public Annotation? Selected=>selected;
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] public RectangleF SelectedScreenBounds=>selected==null?RectangleF.Empty:new RectangleF(View.X+SelectionBounds.X*DisplayScale,View.Y+SelectionBounds.Y*DisplayScale,SelectionBounds.Width*DisplayScale,SelectionBounds.Height*DisplayScale);
    public void ZoomBy(float multiplier)
    {
        if(background==null)return;var pointer=new Point(Width/2,Height/2);var anchor=ToImage(pointer);zoom=Math.Clamp(zoom*multiplier,Math.Min(.02f,Fit)/Fit,32/Fit);pan=ShapeGeometry.ZoomPan(pointer,anchor,DisplayScale,View.Location,pan);Finish(false);
    }
    public event EventHandler? Changed,SelectionChanged;
    public ImageEditor(){DoubleBuffered=true;BackColor=Color.FromArgb(39,43,50);Dock=DockStyle.Fill;TabStop=true;SetStyle(ControlStyles.Selectable,true);}
    public static Rectangle Box(Point a,Point b)=>Rectangle.Round(ShapeGeometry.Box(a,b));
    private float Fit=>background==null?1:Math.Max(.001f,Math.Min((float)Math.Max(1,ClientSize.Width-48)/background.Width,(float)Math.Max(1,ClientSize.Height-48)/background.Height));
    private float DisplayScale=>Fit*zoom;
    private RectangleF View=>background==null?RectangleF.Empty:new RectangleF((Width-background.Width*DisplayScale)/2+pan.X,(Height-background.Height*DisplayScale)/2+pan.Y,background.Width*DisplayScale,background.Height*DisplayScale);
    private PointF ToImage(Point p)=>new((p.X-View.X)/DisplayScale,(p.Y-View.Y)/DisplayScale);
    private PointF Clamped(Point p){var q=ToImage(p);return new(Math.Clamp(q.X,0,background!.Width),Math.Clamp(q.Y,0,background.Height));}
    public void FitView(){zoom=1;pan=PointF.Empty;Finish(false);}
    public void LoadImage(Bitmap image){SetSpacePan(false);counter.Reset();pasteCount=0;ClearHistory();background?.Dispose();annotations.Clear();selected=null;import=null;foreach(var a in assets)a.Dispose();assets.Clear();background=image;previewDirty=true;FitView();SelectionChanged?.Invoke(this,EventArgs.Empty);}
    private void ClearRedo(){foreach(var s in redo)s.Dispose();redo.Clear();}
    private void ClearHistory(){foreach(var s in undo)s.Dispose();undo.Clear();ClearRedo();}
    private State Current()=>new((Bitmap)background!.Clone(),annotations.Select(a=>a.Clone()).ToList(),selection.Select(a=>annotations.IndexOf(a)).ToArray(),counter.Next);
    private void Snapshot(){if(background==null)return;undo.AddLast(Current());ClearRedo();long bytes=undo.Sum(s=>(long)s.Background.Width*s.Background.Height*4);while(bytes>Budget && undo.Count>1){bytes-=(long)undo.First!.Value.Background.Width*undo.First.Value.Background.Height*4;undo.First.Value.Dispose();undo.RemoveFirst();}}
    private void Restore(State state){background?.Dispose();background=state.Background;annotations=state.Annotations;counter.Reset(state.Counter);selection.Clear();selection.AddRange(state.Selected.Where(i=>i>=0&&i<annotations.Count).Select(i=>annotations[i]));SelectionChanged?.Invoke(this,EventArgs.Empty);Finish();}
    public void Undo(){if(background==null || undo.Count==0)return;redo.Push(Current());var state=undo.Last!.Value;undo.RemoveLast();Restore(state);}
    public void Redo(){if(background==null || redo.Count==0)return;undo.AddLast(Current());Restore(redo.Pop());}
    private void Select(Annotation? value){selected=value;SelectionChanged?.Invoke(this,EventArgs.Empty);Finish(false);}
    public void ClearSelection()=>Select(null);
    public void DeleteSelected(){if(selected==null)return;Snapshot();foreach(var a in selection)annotations.Remove(a);Select(null);Finish();}
    public void EditSelectedText(){if(selected?.Kind!=EditTool.Text)return;var value=RequestText?.Invoke(selected.Text);if(value==null)return;Snapshot();selected.Text=value;MeasureText(selected);Finish();}
    public void BringSelectedToFront(){if(selected==null)return;Snapshot();foreach(var a in selection){annotations.Remove(a);annotations.Add(a);}Finish();}
    public void SetImageSticker(Bitmap image){if((long)image.Width*image.Height>4_000_000 || assets.Sum(a=>(long)a.Width*a.Height*4)+(long)image.Width*image.Height*4>32_000_000){image.Dispose();throw new InvalidOperationException("Sticker limit: 4 megapixels each / 32 MB total. Open a new screenshot to clear imports.");}assets.Add(image);import=image;Select(null);Tool=EditTool.ImageSticker;Finish(false);}
    public bool CopySelected()
    {
        if(selected==null)return false;var data=new DataObject();data.SetData(AnnotationTransfer.GroupFormat,false,AnnotationTransfer.SerializeMany(selection));if(selection.Count==1)data.SetData(AnnotationTransfer.Format,false,AnnotationTransfer.Serialize(selected));Clipboard.SetDataObject(data,true);pasteCount=0;return true;
    }
    public bool PasteAnnotation()
    {
        if(background==null)return false;
        List<Annotation> copies;
        if(Clipboard.GetData(AnnotationTransfer.GroupFormat) is string group)copies=AnnotationTransfer.DeserializeMany(group);
        else if(Clipboard.GetData(AnnotationTransfer.Format) is string payload)copies=[AnnotationTransfer.Deserialize(payload)];
        else return false;
        var incoming=copies.Where(a=>a.Asset!=null).Select(a=>a.Asset!).ToList();
        if(incoming.Any(a=>(long)a.Width*a.Height>4_000_000)||assets.Sum(a=>(long)a.Width*a.Height*4)+incoming.Sum(a=>(long)a.Width*a.Height*4)>32_000_000){foreach(var a in incoming)a.Dispose();throw new InvalidOperationException("Pasted stickers exceed document asset limit.");}
        Snapshot();assets.AddRange(incoming);pasteCount++;
        var bounds=SelectionGeometry.Bounds(copies.Select(a=>a.Bounds));var target=bounds;target.Offset(24*pasteCount,24*pasteCount);
        if(!target.IntersectsWith(new RectangleF(0,0,background.Width,background.Height)))target.Location=new PointF(20,20);
        foreach(var copy in copies){copy.Transform(SelectionGeometry.MapBounds(copy.Bounds,bounds,target));annotations.Add(copy);}
        selection.Clear();selection.AddRange(copies);Tool=EditTool.Select;SelectionChanged?.Invoke(this,EventArgs.Empty);Finish();return true;
    }
    private PointF[] Corners(RectangleF r)=>[new(r.Left,r.Top),new(r.Right,r.Top),new(r.Right,r.Bottom),new(r.Left,r.Bottom)];
    private int HandleAt(PointF p){if(selected==null)return -1;var corners=Corners(SelectionBounds);float radius=7/DisplayScale;for(int i=0;i<4;i++)if(Math.Abs(p.X-corners[i].X)<=radius && Math.Abs(p.Y-corners[i].Y)<=radius)return i;return -1;}
    protected override void OnMouseWheel(MouseEventArgs e)
    {
        base.OnMouseWheel(e);if(background==null)return;
        if((ModifierKeys&Keys.Control)!=0){var anchor=ToImage(e.Location);zoom=Math.Clamp(zoom*MathF.Pow(1.2f,e.Delta/120f),Math.Min(.02f,Fit)/Fit,32/Fit);var view=View;pan=ShapeGeometry.ZoomPan(e.Location,anchor,DisplayScale,view.Location,pan);}
        else if((ModifierKeys&Keys.Shift)!=0)pan.X+=e.Delta/2f;else pan.Y+=e.Delta/2f;
        Finish(false);
    }
    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);Focus();if(background==null)return;
        if(e.Button==MouseButtons.Middle || (e.Button==MouseButtons.Left && (spaceHeld || Tool==EditTool.Pan || (ModifierKeys&Keys.Alt)!=0))){panning=true;lastScreen=e.Location;Capture=true;Cursor=Cursors.Hand;return;}
        if(e.Button!=MouseButtons.Left)return;start=Clamped(e.Location);gestureSaved=false;
        var p=ToImage(e.Location);
        handle=Tool==EditTool.Select?HandleAt(p):-1;
        if(handle>=0){resizing=true;originals=selection.Select(a=>a.Clone()).ToList();originalGroup=SelectionBounds;start=p;Capture=true;return;}
        if(Tool==EditTool.Select && (ModifierKeys&Keys.Shift)!=0){selecting=true;start=p;marquee=new RectangleF(p,SizeF.Empty);Select(null);Capture=true;return;}
        if(Tool==EditTool.Select)
        {
            var hit=annotations.LastOrDefault(a=>a.Hit(p,5/DisplayScale));bool ctrl=(ModifierKeys&Keys.Control)!=0;
            togglePending=ctrl && hit!=null && selection.Contains(hit);
            if(hit==null){if(!ctrl)Select(null);return;}
            if(ctrl){if(!selection.Contains(hit))selection.Add(hit);SelectionChanged?.Invoke(this,EventArgs.Empty);Invalidate();}
            else if(!selection.Contains(hit))Select(hit);
            moving=true;originals=selection.Select(a=>a.Clone()).ToList();originalGroup=SelectionBounds;start=p;duplicatePending=ctrl;Capture=true;return;
        }
        if(!View.Contains(e.Location))return;Select(null);
        draft=new Annotation{Kind=Tool,Color=Tool==EditTool.Redact?Color.Black:ink,Stroke=stroke,FontSize=fontSize,Bounds=new RectangleF(start,new SizeF(1,1))};
        if(Tool is EditTool.Text or EditTool.Sticker or EditTool.ImageSticker or EditTool.Counter)
        {
            if(Tool==EditTool.Counter){draft.Text=counter.Next.ToString();draft.Bounds=new(start,new SizeF(counterSize,counterSize));draft.FontSize=24;Snapshot();counter.Take();annotations.Add(draft);selected=draft;draft=null;SelectionChanged?.Invoke(this,EventArgs.Empty);Finish();return;}
            if(Tool==EditTool.Text){var value=RequestText?.Invoke(null);if(string.IsNullOrEmpty(value)){draft=null;return;}draft.Text=value;draft.TextBackground=textBackground;MeasureText(draft);draft.SetTextBorder(borderStyle,borderWidth,borderColor);}
            else if(Tool==EditTool.Sticker){draft.Text=Sticker;MeasureText(draft);}
            else{if(import==null){draft=null;return;}draft.Asset=import;float factor=Math.Min(1,128f/Math.Max(import.Width,import.Height));draft.Bounds=new(start,new SizeF(import.Width*factor,import.Height*factor));}
            Snapshot();annotations.Add(draft);selected=draft;draft=null;Tool=EditTool.Select;SelectionChanged?.Invoke(this,EventArgs.Empty);Finish();return;
        }
        drawing=true;draft.Points.Add(start);Capture=true;
    }
    private void MeasureText(Annotation item){using var font=new Font(item.Kind==EditTool.Sticker?"Segoe UI Emoji":"Segoe UI",item.FontSize,FontStyle.Bold,GraphicsUnit.Pixel);using var g=CreateGraphics();var size=g.MeasureString(item.Text,font,10000,StringFormat.GenericTypographic);float padding=item.TextPadding*2;item.Bounds=new(item.Bounds.Location,new SizeF(Math.Max(8,size.Width+8+padding),Math.Max(8,size.Height+8+padding)));}
    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);if(background==null)return;
        if(panning){pan.X+=e.X-lastScreen.X;pan.Y+=e.Y-lastScreen.Y;lastScreen=e.Location;Finish(false);return;}
        var p=ToImage(e.Location);
        if(selecting){marquee=ShapeGeometry.Box(start,p);Invalidate();return;}
        if((moving || resizing) && selection.Count>0 && originals.Count==selection.Count)
        {
            if(!gestureSaved)
            {
                if(Math.Abs(p.X-start.X)+Math.Abs(p.Y-start.Y)<2/DisplayScale)return;
                Snapshot();gestureSaved=true;
                if(duplicatePending && moving){var copies=selection.Select(a=>a.Clone()).ToList();annotations.AddRange(copies);selection.Clear();selection.AddRange(copies);duplicatePending=false;}
            }
            var target=resizing?ShapeGeometry.Resize(originalGroup,handle,p,(ModifierKeys&Keys.Shift)!=0):new RectangleF(originalGroup.X+p.X-start.X,originalGroup.Y+p.Y-start.Y,originalGroup.Width,originalGroup.Height);
            for(int i=0;i<selection.Count;i++)
            {
                var a=selection[i];var source=originals[i];a.Points=new(source.Points);a.Bounds=source.Bounds;a.FontSize=source.FontSize;
                a.Transform(SelectionGeometry.MapBounds(source.Bounds,originalGroup,target),resizing);
            }
            Finish();return;
        }
        if(drawing && draft!=null){var end=Clamped(e.Location);draft.Bounds=ShapeGeometry.Box(start,end);if(Tool==EditTool.Pen){draft.Points.Add(end);float left=draft.Points.Min(x=>x.X),top=draft.Points.Min(x=>x.Y);draft.Bounds=new(left,top,Math.Max(1,draft.Points.Max(x=>x.X)-left),Math.Max(1,draft.Points.Max(x=>x.Y)-top));}else if(Tool==EditTool.Arrow){draft.Points=[start,end];draft.Bounds=new(draft.Bounds.X,draft.Bounds.Y,Math.Max(1,draft.Bounds.Width),Math.Max(1,draft.Bounds.Height));}Invalidate();}
        else if(spaceHeld)Cursor=Cursors.Hand;else if(Tool==EditTool.Select)Cursor=HandleAt(p)>=0?Cursors.SizeAll:Cursors.Default;
    }
    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);if(drawing || moving || resizing)OnMouseMove(e);Capture=false;bool wasDrawing=drawing;
        if(selecting){selection.Clear();selection.AddRange(annotations.Where(a=>marquee.Contains(a.Bounds)));selecting=false;marquee=RectangleF.Empty;}
        else if(moving && !gestureSaved && togglePending && selected!=null)selection.Remove(annotations.LastOrDefault(a=>a.Hit(ToImage(e.Location),5/DisplayScale))!);
        togglePending=duplicatePending=false;drawing=moving=resizing=panning=false;Cursor=spaceHeld || Tool==EditTool.Pan?Cursors.Hand:Tool==EditTool.Select?Cursors.Default:Cursors.Cross;
        if(!wasDrawing || draft==null || background==null){originals.Clear();SelectionChanged?.Invoke(this,EventArgs.Empty);Finish(false);return;}
        var item=draft;draft=null;
        if(item.Kind!=EditTool.Pen && (item.Bounds.Width<2 || item.Bounds.Height<2) && item.Kind!=EditTool.Arrow){Invalidate();return;}
        Snapshot();
        if(item.Kind==EditTool.Crop)
        {
            var bounds=Rectangle.Intersect(Rectangle.Round(item.Bounds),new Rectangle(Point.Empty,background.Size));
            if(bounds.Width>0 && bounds.Height>0){var cropped=background.Clone(bounds,PixelFormat.Format32bppArgb);background.Dispose();background=cropped;foreach(var a in annotations)a.Transform(new RectangleF(a.Bounds.X-bounds.X,a.Bounds.Y-bounds.Y,a.Bounds.Width,a.Bounds.Height));annotations.RemoveAll(a=>!a.Bounds.IntersectsWith(new RectangleF(PointF.Empty,background.Size)));selected=null;FitView();}
        }
        else{if(item.Kind==EditTool.Arrow && item.Points.Count<2)item.Points=[start,Clamped(e.Location)];annotations.Add(item);selected=item;Tool=EditTool.Select;}
        SelectionChanged?.Invoke(this,EventArgs.Empty);Finish();
    }
    public Bitmap RenderImage()
    {
        if(background==null)throw new InvalidOperationException("Open or capture an image first.");var result=new Bitmap(background.Width,background.Height,PixelFormat.Format32bppArgb);
        try{using var g=Graphics.FromImage(result);g.DrawImageUnscaled(background,0,0);g.SmoothingMode=SmoothingMode.AntiAlias;g.TextRenderingHint=System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
            foreach(var a in annotations){if(a.Kind==EditTool.Pixelate)Pixelate(result,g,a.Bounds);else a.Draw(g);}return result;
        }catch{result.Dispose();throw;}
    }
    private static void Pixelate(Bitmap image,Graphics target,RectangleF box)
    {
        var r=Rectangle.Intersect(Rectangle.Round(box),new Rectangle(Point.Empty,image.Size));if(r.Width<1 || r.Height<1)return;
        using var source=image.Clone(r,PixelFormat.Format32bppArgb);using var small=new Bitmap(Math.Max(1,r.Width/14),Math.Max(1,r.Height/14));
        using(var g=Graphics.FromImage(small)){g.InterpolationMode=InterpolationMode.HighQualityBilinear;g.DrawImage(source,new Rectangle(Point.Empty,small.Size));}
        var state=target.Save();target.InterpolationMode=InterpolationMode.NearestNeighbor;target.PixelOffsetMode=PixelOffsetMode.Half;target.DrawImage(small,r);target.Restore(state);
    }
    private void Finish(bool imageChanged=true){if(imageChanged)previewDirty=true;if(!GestureActive)Changed?.Invoke(this,EventArgs.Empty);Invalidate();}
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);if(background==null){TextRenderer.DrawText(e.Graphics,"Capture a screen or paste an image with Ctrl+V",Font,ClientRectangle,ForeColor,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);PaintCardShadows(e.Graphics);return;}
        if(previewDirty || preview==null){preview?.Dispose();preview=RenderImage();previewDirty=false;}var rendered=preview;var v=View;e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
        using(var shadow=Theme.Round(new RectangleF(v.X+3,v.Y+6,v.Width,v.Height),10))using(var brush=new SolidBrush(Color.FromArgb(18,50,64,94)))e.Graphics.FillPath(brush,shadow);
        var clip=e.Graphics.Save();using(var paper=Theme.Round(v,10))e.Graphics.SetClip(paper);e.Graphics.InterpolationMode=InterpolationMode.HighQualityBicubic;e.Graphics.DrawImage(rendered,v);e.Graphics.Restore(clip);
        var state=e.Graphics.Save();e.Graphics.SetClip(v);e.Graphics.TranslateTransform(v.X,v.Y);e.Graphics.ScaleTransform(DisplayScale,DisplayScale);
        if(draft!=null){if(draft.Kind is EditTool.Crop or EditTool.Pixelate){using var pen=new Pen(Color.DeepSkyBlue,2/DisplayScale){DashStyle=DashStyle.Dash};e.Graphics.DrawRectangle(pen,draft.Bounds.X,draft.Bounds.Y,draft.Bounds.Width,draft.Bounds.Height);}else draft.Draw(e.Graphics);}
        if(selecting){using var fill=new SolidBrush(Color.FromArgb(35,Theme.Accent));using var edge=new Pen(Theme.Accent,1/DisplayScale){DashStyle=DashStyle.Dash};e.Graphics.FillRectangle(fill,marquee);e.Graphics.DrawRectangle(edge,marquee.X,marquee.Y,marquee.Width,marquee.Height);}
        e.Graphics.Restore(state);
        if(selected!=null){var b=SelectionBounds;var screen=new RectangleF(v.X+b.X*DisplayScale,v.Y+b.Y*DisplayScale,Math.Max(1,b.Width*DisplayScale),Math.Max(1,b.Height*DisplayScale));using var pen=new Pen(Color.DeepSkyBlue,1){DashStyle=DashStyle.Dash};e.Graphics.DrawRectangle(pen,screen.X,screen.Y,screen.Width,screen.Height);foreach(var corner in Corners(screen)){e.Graphics.FillEllipse(Brushes.White,corner.X-4,corner.Y-4,8,8);e.Graphics.DrawEllipse(Pens.RoyalBlue,corner.X-4,corner.Y-4,8,8);}}
        if(selection.Count>1){using var edge=new Pen(Color.FromArgb(150,Theme.Accent)){DashStyle=DashStyle.Dot};foreach(var a in selection){var r=a.Bounds;e.Graphics.DrawRectangle(edge,v.X+r.X*DisplayScale,v.Y+r.Y*DisplayScale,r.Width*DisplayScale,r.Height*DisplayScale);}}
        PaintCardShadows(e.Graphics);
    }
    private void PaintCardShadows(Graphics graphics)
    {
        if(Parent==null)return;graphics.SmoothingMode=SmoothingMode.AntiAlias;float scale=DeviceDpi/96f;
        // Shadows are canvas pixels; cards are independent, opaque windows above
        // them. Moving an annotation never invalidates or repaints the controls.
        foreach(var card in Parent.Controls.OfType<RoundedCard>().Where(c=>c.Visible))
        for(int i=6;i>=1;i--){float spread=i*scale;using var path=Theme.Round(new RectangleF(card.Left-Left-spread,card.Top-Top+3*scale-spread,card.Width+2*spread,card.Height+2*spread),13*scale+spread);using var brush=new SolidBrush(Color.FromArgb(4,35,48,74));graphics.FillPath(brush,path);}
    }
    protected override void OnMouseDoubleClick(MouseEventArgs e){base.OnMouseDoubleClick(e);if(Tool==EditTool.Select)EditSelectedText();}
    protected override void OnResize(EventArgs e){base.OnResize(e);Invalidate();}
    protected override void Dispose(bool disposing){if(disposing){background?.Dispose();preview?.Dispose();ClearHistory();foreach(var asset in assets)asset.Dispose();}base.Dispose(disposing);}
}
