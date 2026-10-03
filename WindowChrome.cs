using System.Runtime.InteropServices;
namespace ScreenAnote;
internal static class WindowChrome
{
    [DllImport("user32.dll")]private static extern bool ReleaseCapture();
    [DllImport("user32.dll")]private static extern IntPtr SendMessage(IntPtr window,int message,IntPtr wparam,IntPtr lparam);
    [DllImport("dwmapi.dll")]private static extern int DwmSetWindowAttribute(IntPtr window,int attribute,ref int value,int size);
    public static void Drag(Form window){ReleaseCapture();SendMessage(window.Handle,0xA1,(IntPtr)2,IntPtr.Zero);}
    public static void Round(Form window){int round=2;DwmSetWindowAttribute(window.Handle,33,ref round,4);}
}
