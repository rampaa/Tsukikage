using Tsukikage.Interop;

namespace Tsukikage;

internal static class MagpieUtils
{
    private static bool s_isMagpieScaling;

    public static int MagpieScalingChangedWindowMessage { get; private set; } = -1;

    private static nint s_magpieWindowHandle;

    private static bool IsMagpieScaling()
    {
        if (s_isMagpieScaling)
        {
            s_isMagpieScaling = IsMagpieReallyScaling();
        }

        return s_isMagpieScaling;
    }

    private static Rectangle s_sourceWindowRect;

    private static Rectangle MagpieWindowRect { get; set; }
    // public static nint SourceWindowHandle { get; set; }

    private static float s_scaleFactorX;
    private static float s_scaleFactorY;

    public static void Init()
    {
        nint magpieWindowHandle = GetMagpieWindowHandle();

        bool isMagpieScaling = magpieWindowHandle is not 0;
        s_isMagpieScaling = isMagpieScaling;

        if (isMagpieScaling)
        {
            SetMagpieInfo(magpieWindowHandle);
        }
    }

    public static void RegisterToMagpieScalingChangedMessage(nint windowHandle)
    {
        MagpieScalingChangedWindowMessage = WinApi.RegisterToWindowMessage("MagpieScalingChanged");
        _ = WinApi.AllowWindowMessage(windowHandle, MagpieScalingChangedWindowMessage);
    }

    private static int GetMagpieWindowLeftEdgePositionFromMagpie(nint windowHandle)
    {
        return (int)WinApi.GetProp(windowHandle, "Magpie.DestLeft");
    }

    private static int GetMagpieWindowRightEdgePositionFromMagpie(nint windowHandle)
    {
        return (int)WinApi.GetProp(windowHandle, "Magpie.DestRight");
    }

    private static int GetMagpieWindowTopEdgePositionFromMagpie(nint windowHandle)
    {
        return (int)WinApi.GetProp(windowHandle, "Magpie.DestTop");
    }

    private static int GetMagpieWindowBottomEdgePositionFromMagpie(nint windowHandle)
    {
        return (int)WinApi.GetProp(windowHandle, "Magpie.DestBottom");
    }

    private static int GetSourceWindowLeftEdgePositionFromMagpie(nint windowHandle)
    {
        return (int)WinApi.GetProp(windowHandle, "Magpie.SrcLeft");
    }

    private static int GetSourceWindowTopEdgePositionFromMagpie(nint windowHandle)
    {
        return (int)WinApi.GetProp(windowHandle, "Magpie.SrcTop");
    }

    private static int GetSourceWindowRightEdgePositionFromMagpie(nint windowHandle)
    {
        return (int)WinApi.GetProp(windowHandle, "Magpie.SrcRight");
    }

    private static int GetSourceWindowBottomEdgePositionFromMagpie(nint windowHandle)
    {
        return (int)WinApi.GetProp(windowHandle, "Magpie.SrcBottom");
    }

    //public static nint GetSourceWindowHande(nint windowHandle)
    //{
    //    return WinApi.GetProp(windowHandle, "Magpie.SrcHWND");
    //}

    /// <summary>
    /// If Magpie crashes or is killed during the process of scaling a window, the MagpieScalingChangedWindowMessage will not be received.
    /// Consequently, IsMagpieScaling may not be set to false.
    /// To ensure Magpie is still running, this method must be used to re-check whether any window is currently being scaled by Magpie.
    /// </summary>
    private static bool IsMagpieReallyScaling()
    {
        s_magpieWindowHandle = GetMagpieWindowHandle();
        return s_magpieWindowHandle is not 0;
    }

    private static nint GetMagpieWindowHandle()
    {
        return WinApi.FindWindow("Window_Magpie_967EB565-6F73-4E94-AE53-00CC42592A22");
    }

    public static Point GetMousePosition(Point mousePosition)
    {
        if (!IsMagpieScaling() || !MagpieWindowRect.Contains(mousePosition))
        {
            return mousePosition;
        }

        Point virtualMousePosition = new(float.ConvertToIntegerNative<int>(s_sourceWindowRect.X + ((mousePosition.X - MagpieWindowRect.X) / s_scaleFactorX)),
            float.ConvertToIntegerNative<int>(s_sourceWindowRect.Y + ((mousePosition.Y - MagpieWindowRect.Y) / s_scaleFactorY)));

        return !s_sourceWindowRect.Contains(virtualMousePosition) || WinApi.GetWindowHandleFromPoint(mousePosition) != s_magpieWindowHandle
            ? mousePosition
            : virtualMousePosition;
    }

    public static bool IsMouseDirectlyOver(Point rawMousePosition, Point modifiedMousePosition, nint windowHandle)
    {
        if (WinApi.GetTopmostWindow() == windowHandle)
        {
            return true;
        }

        nint windowHandleAtPoint = WinApi.GetWindowHandleFromPoint(rawMousePosition);
        return windowHandleAtPoint is not 0
                && (rawMousePosition == modifiedMousePosition
                    ? windowHandleAtPoint == windowHandle
                    : windowHandleAtPoint == s_magpieWindowHandle || WinApi.GetWindowHandleFromPoint(modifiedMousePosition) != s_magpieWindowHandle);
    }

    public static void SetMagpieInfo(nint wParam, nint lParam)
    {
        if (wParam is 0)
        {
            s_isMagpieScaling = lParam is 1;
        }
        else if (wParam is 1 or 2)
        {
            s_isMagpieScaling = true;
            SetMagpieInfo(wParam is 1 ? lParam : s_magpieWindowHandle);
        }
    }

    private static void SetMagpieInfo(nint magpieWindowHandle)
    {
        int magpieWindowTopEdgePosition = GetMagpieWindowTopEdgePositionFromMagpie(magpieWindowHandle);
        int magpieWindowBottomEdgePosition = GetMagpieWindowBottomEdgePositionFromMagpie(magpieWindowHandle);
        int magpieWindowLeftEdgePosition = GetMagpieWindowLeftEdgePositionFromMagpie(magpieWindowHandle);
        int magpieWindowRightEdgePosition = GetMagpieWindowRightEdgePositionFromMagpie(magpieWindowHandle);
        int magpieWindowWidth = magpieWindowRightEdgePosition - magpieWindowLeftEdgePosition;
        int magpieWindowHeight = magpieWindowBottomEdgePosition - magpieWindowTopEdgePosition;

        if (magpieWindowWidth is 0 || magpieWindowHeight is 0)
        {
            return;
        }

        MagpieWindowRect = new Rectangle(magpieWindowLeftEdgePosition, magpieWindowTopEdgePosition, magpieWindowWidth, magpieWindowHeight);

        int sourceWindowLeftEdgePosition = GetSourceWindowLeftEdgePositionFromMagpie(magpieWindowHandle);
        int sourceWindowTopEdgePosition = GetSourceWindowTopEdgePositionFromMagpie(magpieWindowHandle);
        int sourceWindowRightEdgePosition = GetSourceWindowRightEdgePositionFromMagpie(magpieWindowHandle);
        int sourceWindowBottomEdgePosition = GetSourceWindowBottomEdgePositionFromMagpie(magpieWindowHandle);
        int sourceWindowWidth = sourceWindowRightEdgePosition - sourceWindowLeftEdgePosition;
        int sourceWindowHeight = sourceWindowBottomEdgePosition - sourceWindowTopEdgePosition;
        s_sourceWindowRect = new Rectangle(sourceWindowLeftEdgePosition, sourceWindowTopEdgePosition, sourceWindowWidth, sourceWindowHeight);

        s_scaleFactorX = (float)magpieWindowWidth / sourceWindowWidth;
        s_scaleFactorY = (float)magpieWindowHeight / sourceWindowHeight;
        // SourceWindowHandle = GetSourceWindowHande(lParam);
    }
}
