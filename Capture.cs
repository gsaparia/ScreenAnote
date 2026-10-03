using System.Runtime.InteropServices;
using System.Text;
using System.Drawing.Imaging;

namespace ScreenAnote;
internal static class Native
{
    internal delegate bool EnumProc(IntPtr h, IntPtr param);
    [DllImport("user32.dll")] internal static extern bool EnumWindows(EnumProc callback, IntPtr param);
    [DllImport("user32.dll")] internal static extern bool IsWindowVisible(IntPtr h);
    [DllImport("user32.dll", CharSet=CharSet.Unicode)] internal static extern int GetWindowText(IntPtr h, StringBuilder text, int max);
    [DllImport("user32.dll")] internal static extern bool GetWindowRect(IntPtr h, out Rect r);
    [DllImport("user32.dll")] internal static extern bool SetForegroundWindow(IntPtr h);
    [DllImport("user32.dll")] internal static extern bool ShowWindow(IntPtr h, int cmd);
    [DllImport("user32.dll")] internal static extern short GetAsyncKeyState(int key);
    [DllImport("user32.dll")] internal static extern void mouse_event(uint flags,uint x,uint y,int data,UIntPtr extra);
    [StructLayout(LayoutKind.Sequential)] internal struct Rect { public int Left,Top,Right,Bottom; public Rectangle Bounds => Rectangle.FromLTRB(Left,Top,Right,Bottom); }
}
internal static class ScreenCapture
{
    public static Bitmap Screen(Rectangle area)
    {
        if (area.Width < 2 || area.Height < 2) throw new InvalidOperationException("Select a larger area.");
        if ((long)area.Width * area.Height > 40_000_000) throw new InvalidOperationException("Capture exceeds the 40 megapixel limit.");
        var image = new Bitmap(area.Width, area.Height, PixelFormat.Format32bppArgb);
        try { using var g=Graphics.FromImage(image); g.CopyFromScreen(area.Location, Point.Empty, area.Size, CopyPixelOperation.SourceCopy); return image; }
        catch { image.Dispose(); throw; }
    }
    public static Rectangle? SelectRegion()
    {
        using var picker=new RegionPicker();
        return picker.ShowDialog()==DialogResult.OK ? picker.Selected : null;
    }
    public static IntPtr PickWindow()
    {
        using var dialog=new Form {Text="Choose window",Width=620,Height=420,StartPosition=FormStartPosition.CenterScreen};
        var list=new ListBox {Dock=DockStyle.Fill};
        Native.EnumWindows((h,_) => {
            var title=new StringBuilder(512); Native.GetWindowText(h,title,title.Capacity);
            if(Native.IsWindowVisible(h) && title.Length>0 && !title.ToString().StartsWith("ScreenAnote")) list.Items.Add(new WindowItem(h,title.ToString()));
            return true;
        },IntPtr.Zero);
        var ok=new Button {Text="Capture selected window",Dock=DockStyle.Bottom,Height=40,DialogResult=DialogResult.OK};
        dialog.Controls.Add(list); dialog.Controls.Add(ok); dialog.AcceptButton=ok;
        list.DoubleClick+=(_,_)=>{if(list.SelectedItem!=null) dialog.DialogResult=DialogResult.OK;};
        return dialog.ShowDialog()==DialogResult.OK && list.SelectedItem is WindowItem item ? item.Handle : IntPtr.Zero;
    }
    private record WindowItem(IntPtr Handle,string Title) { public override string ToString()=>Title; }
}
internal sealed class RegionPicker:Form
{
    private readonly Bitmap background;
    private Point start,current;
    private bool dragging;
    public Rectangle Selected {get;private set;}
    public RegionPicker()
    {
        var bounds=SystemInformation.VirtualScreen;
        background=global::ScreenAnote.ScreenCapture.Screen(bounds); Bounds=bounds;
        FormBorderStyle=FormBorderStyle.None; StartPosition=FormStartPosition.Manual;
        TopMost=true; DoubleBuffered=true; Cursor=Cursors.Cross; KeyPreview=true;
        KeyDown+=(_,e)=>{if(e.KeyCode==Keys.Escape) DialogResult=DialogResult.Cancel;};
        MouseDown+=(_,e)=>{if(e.Button!=MouseButtons.Left)return; start=current=e.Location; dragging=true; Capture=true;};
        MouseMove+=(_,e)=>{current=e.Location;Invalidate();};
        MouseUp+=(_,e)=>{if(!dragging)return; current=e.Location; dragging=false; Capture=false;
            var r=ImageEditor.Box(start,current); if(r.Width<2 || r.Height<2)return;
            Selected=new Rectangle(r.X+Left,r.Y+Top,r.Width,r.Height); DialogResult=DialogResult.OK;};
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.DrawImageUnscaled(background,0,0);
        using var shade=new SolidBrush(Color.FromArgb(110,0,0,0)); e.Graphics.FillRectangle(shade,ClientRectangle);
        if(dragging){var r=ImageEditor.Box(start,current);e.Graphics.DrawImage(background,r,r,GraphicsUnit.Pixel);using var p=new Pen(Color.DeepSkyBlue,2);e.Graphics.DrawRectangle(p,r);}
        using var f=new Font("Segoe UI",14); e.Graphics.DrawString("Drag to select • Esc to cancel",f,Brushes.White,20,20);
    }
    protected override void Dispose(bool disposing){if(disposing)background.Dispose();base.Dispose(disposing);}
}
