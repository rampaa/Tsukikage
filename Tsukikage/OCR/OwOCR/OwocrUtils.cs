using Tsukikage.Interop;

namespace Tsukikage.OCR.OwOCR;

internal static class OwocrUtils
{
    private const string OwocrRunningEventName = "owocr_running";

    public static void HandleOwocrExiting()
    {
        OcrUtils.OcrResult = null;
        OcrUtils.SendEmptyString();
    }

    public static bool IsOwocrStopped()
    {
        return WinApi.IsEventStopped(OwocrRunningEventName);
    }
}
