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

    public static async Task TriggerOcr()
    {
        if (ConfigManager.TimeToWaitAfterReceivingTextFromTextHookerBeforeTriggeringOcrInMilliseconds > 0)
        {
            await Task.Delay(ConfigManager.TimeToWaitAfterReceivingTextFromTextHookerBeforeTriggeringOcrInMilliseconds).ConfigureAwait(false);
        }

        // TODO: Trigger OCR process
    }
}
