namespace Tsukikage;

internal static class AppInfo
{
    private static readonly string s_applicationPath = AppContext.BaseDirectory;
    public static readonly string ConfigFilePath = Path.Join(s_applicationPath, "Tsukikage.ini");
}
