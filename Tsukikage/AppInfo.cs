namespace Tsukikage;

internal static class AppInfo
{
    public static readonly string ApplicationPath = AppContext.BaseDirectory;
    public static readonly string ConfigFilePath = Path.Join(ApplicationPath, "Tsukikage.ini");
}
