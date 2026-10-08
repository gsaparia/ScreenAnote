namespace ScreenAnote;
internal static class AppIcon
{
    public static Icon Load()
    {
        using var stream=typeof(AppIcon).Assembly.GetManifestResourceStream("ScreenAnote.Assets.ScreenAnote.ico")!;
        using var icon=new Icon(stream);return (Icon)icon.Clone();
    }
}
