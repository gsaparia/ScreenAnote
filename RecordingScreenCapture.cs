using System.ComponentModel;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
namespace ScreenAnote;
internal static class RecordingScreenCapture
{
    // Capture into a native, screen-compatible memory DC and top-down DIB.
    // GDI+ receives the pixels only after BitBlt has finished; no Graphics.GetHdc
    // handle is used as the screen-copy destination.
    public static void CopyFrame(Bitmap destination,Rectangle area,CursorOverlay? cursor=null)
    {
        if(destination.Size!=area.Size)throw new ArgumentException("Recording frame size does not match the selected region.");
        for(int attempt=0;;attempt++)
        {
            try{CopyOnce(destination,area,cursor);return;}
            catch(Win32Exception ex) when(attempt==0 && ex.NativeErrorCode is 6 or 5)
            {Thread.Sleep(80);}
        }
    }
    private static Win32Exception Failure(string stage,Rectangle area)
    {
        int code=Marshal.GetLastPInvokeError();
        return new Win32Exception(code,$"Recording capture failed at {stage} (Windows error {code}; region {area.X},{area.Y}, {area.Width} × {area.Height}).");
    }
    private static void CopyOnce(Bitmap destination,Rectangle area,CursorOverlay? cursor)
    {
        nint source=GetDC(0);if(source==0)throw Failure("GetDC",area);
        nint memory=0,bitmap=0,previous=0;
        try
        {
            memory=CreateCompatibleDC(source);if(memory==0)throw Failure("CreateCompatibleDC",area);
            var info=new BitmapInfo{Header=new BitmapInfoHeader{Size=(uint)Marshal.SizeOf<BitmapInfoHeader>(),Width=area.Width,Height=-area.Height,Planes=1,BitCount=32,Compression=0}};
            bitmap=CreateDIBSection(source,ref info,0,out nint pixels,0,0);
            if(bitmap==0||pixels==0)throw Failure("CreateDIBSection",area);
            previous=SelectObject(memory,bitmap);
            if(previous==0||previous==-1)throw Failure("SelectObject",area);
            if(!BitBlt(memory,0,0,area.Width,area.Height,source,area.X,area.Y,0x00CC0020))throw Failure("BitBlt",area);
            cursor?.Draw(memory,area);
            // GDI batching must finish before reading the DIB's memory.
            if(!GdiFlush())throw Failure("GdiFlush",area);
            int bytes=checked(area.Width*4);var row=new byte[bytes];
            var locked=destination.LockBits(new Rectangle(Point.Empty,destination.Size),ImageLockMode.WriteOnly,PixelFormat.Format32bppRgb);
            try{for(int y=0;y<area.Height;y++){Marshal.Copy(pixels+y*bytes,row,0,bytes);Marshal.Copy(row,0,locked.Scan0+y*locked.Stride,bytes);}}
            finally{destination.UnlockBits(locked);}
        }
        finally
        {
            if(memory!=0&&previous!=0&&previous!=-1)SelectObject(memory,previous);
            if(bitmap!=0)DeleteObject(bitmap);
            if(memory!=0)DeleteDC(memory);
            ReleaseDC(0,source);
        }
    }
    internal static Point CursorLocation(Rectangle area,Point screen,Point hotspot)=>new(screen.X-area.X-hotspot.X,screen.Y-area.Y-hotspot.Y);
    // Per-session ownership: cache only the current shape, while reading position
    // and visibility every frame. OS-owned cursor handles are never destroyed.
    internal sealed class CursorOverlay:IDisposable
    {
        private nint sourceCursor,ownedCursor;
        private Point hotspot;
        public void Draw(nint dc,Rectangle area)
        {
            var cursor=new CursorInfo{Size=(uint)Marshal.SizeOf<CursorInfo>()};
            if(!GetCursorInfo(ref cursor)||(cursor.Flags&1)==0||cursor.Cursor==0)return;
            if(cursor.Cursor!=sourceCursor||ownedCursor==0)
            {
                nint copy=CopyIcon(cursor.Cursor);if(copy==0)return;
                var info=new IconInfo();
                try
                {
                    if(!GetIconInfo(copy,out info))return;
                    Dispose();ownedCursor=copy;copy=0;sourceCursor=cursor.Cursor;
                    hotspot=new Point((int)info.HotspotX,(int)info.HotspotY);
                }
                finally
                {
                    if(info.Mask!=0)DeleteObject(info.Mask);if(info.Color!=0)DeleteObject(info.Color);
                    if(copy!=0)DestroyIcon(copy);
                }
            }
            var location=CursorLocation(area,cursor.Position,hotspot);
            // The native bitmap clips at region edges. Draw before resizing so
            // cursor coordinates and size scale with the captured screen image.
            // A disappearing/replaced cursor must not abort a video recording.
            if(!DrawIconEx(dc,location.X,location.Y,ownedCursor,0,0,0,0,3))sourceCursor=0;
        }
        public void Dispose(){if(ownedCursor!=0)DestroyIcon(ownedCursor);ownedCursor=sourceCursor=0;}
    }
    [StructLayout(LayoutKind.Sequential)]private struct CursorInfo{public uint Size,Flags;public nint Cursor;public Point Position;}
    [StructLayout(LayoutKind.Sequential)]private struct IconInfo{public int IsIcon;public uint HotspotX,HotspotY;public nint Mask,Color;}
    [DllImport("user32.dll")][return:MarshalAs(UnmanagedType.Bool)]private static extern bool GetCursorInfo(ref CursorInfo cursor);
    [DllImport("user32.dll")]private static extern nint CopyIcon(nint icon);
    [DllImport("user32.dll")][return:MarshalAs(UnmanagedType.Bool)]private static extern bool GetIconInfo(nint icon,out IconInfo info);
    [DllImport("user32.dll")][return:MarshalAs(UnmanagedType.Bool)]private static extern bool DrawIconEx(nint dc,int x,int y,nint icon,int width,int height,uint step,nint brush,uint flags);
    [DllImport("user32.dll")][return:MarshalAs(UnmanagedType.Bool)]private static extern bool DestroyIcon(nint icon);
    [StructLayout(LayoutKind.Sequential)]private struct BitmapInfoHeader
    {public uint Size;public int Width,Height;public ushort Planes,BitCount;public uint Compression,SizeImage;public int XPelsPerMeter,YPelsPerMeter;public uint ClrUsed,ClrImportant;}
    [StructLayout(LayoutKind.Sequential)]private struct BitmapInfo{public BitmapInfoHeader Header;public uint Color;}
    [DllImport("user32.dll",SetLastError=true)]private static extern nint GetDC(nint window);
    [DllImport("user32.dll")]private static extern int ReleaseDC(nint window,nint dc);
    [DllImport("gdi32.dll",SetLastError=true)]private static extern nint CreateCompatibleDC(nint dc);
    [DllImport("gdi32.dll",SetLastError=true)]private static extern nint CreateDIBSection(nint dc,ref BitmapInfo info,uint usage,out nint pixels,nint section,uint offset);
    [DllImport("gdi32.dll",SetLastError=true)]private static extern nint SelectObject(nint dc,nint value);
    [DllImport("gdi32.dll")][return:MarshalAs(UnmanagedType.Bool)]private static extern bool DeleteObject(nint value);
    [DllImport("gdi32.dll")][return:MarshalAs(UnmanagedType.Bool)]private static extern bool DeleteDC(nint dc);
    [DllImport("gdi32.dll",SetLastError=true)][return:MarshalAs(UnmanagedType.Bool)]private static extern bool GdiFlush();
    [DllImport("gdi32.dll",SetLastError=true)][return:MarshalAs(UnmanagedType.Bool)]private static extern bool BitBlt(nint target,int x,int y,int width,int height,nint source,int sourceX,int sourceY,uint operation);
}
