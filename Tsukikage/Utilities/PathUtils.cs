namespace Tsukikage.Utilities;

internal static class PathUtils
{
    private const string TempFileExtension = ".tmp";

    public static string GetTempPath(string path)
    {
        return $"{path}{TempFileExtension}";
    }

    public static void ReplaceFileAtomicallyOnSameVolume(string fileToBeReplaced, string tempFile)
    {
        if (OperatingSystem.IsWindows() && File.Exists(fileToBeReplaced))
        {
            File.Replace(tempFile, fileToBeReplaced, null, true);
        }
        else
        {
            File.Move(tempFile, fileToBeReplaced, true);
        }
    }
}
