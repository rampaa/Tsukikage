using System.Diagnostics;
using Tsukikage.Interop;

namespace Tsukikage;

internal static class MagpieUtils
{
    private static bool IsMagpieScaling { get; set; }
    public static int MagpieScalingChangedWindowMessage { get; private set; } = -1;

    private static nint s_magpieWindowHandle;

    private static Process? s_magpieProcess;

    private static Rectangle s_sourceWindowRect;

    private static Rectangle MagpieWindowRect { get; set; }
    // public static nint SourceWindowHandle { get; set; }

    private static float s_scaleFactorX;
    private static float s_scaleFactorY;

    public static void Init()
    {
        nint magpieWindowHandle = GetMagpieWindowHandle();
        s_magpieWindowHandle = magpieWindowHandle;

        bool isMagpieScaling = magpieWindowHandle is not 0;
        IsMagpieScaling = isMagpieScaling;

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

    private static nint GetMagpieWindowHandle()
    {
        return WinApi.FindWindow("Window_Magpie_967EB565-6F73-4E94-AE53-00CC42592A22");
    }

    public static Point GetMousePosition(Point mousePosition)
    {
        if (!IsMagpieScaling || !MagpieWindowRect.Contains(mousePosition))
        {
            return mousePosition;
        }

        Point virtualMousePosition = new(float.ConvertToIntegerNative<int>(s_sourceWindowRect.X + ((mousePosition.X - MagpieWindowRect.X) / s_scaleFactorX)),
            float.ConvertToIntegerNative<int>(s_sourceWindowRect.Y + ((mousePosition.Y - MagpieWindowRect.Y) / s_scaleFactorY)));

        return !s_sourceWindowRect.Contains(virtualMousePosition) || WinApi.GetWindowFromPoint(mousePosition) != s_magpieWindowHandle
            ? mousePosition
            : virtualMousePosition;
    }

    public static void SetMagpieInfo(nint wParam, nint lParam)
    {
        if (s_magpieProcess is null)
        {
            nint magpieWindowHandle = GetMagpieWindowHandle();

            s_magpieWindowHandle = magpieWindowHandle;
            s_magpieProcess = WinApi.GetProcessByWindowHandle(magpieWindowHandle);
            if (s_magpieProcess is not null)
            {
                s_magpieProcess.Exited += MagpieProcess_Exited;
            }
        }

        if (wParam is 0)
        {
            IsMagpieScaling = lParam is 1;
        }
        else if (wParam is 1 or 2)
        {
            IsMagpieScaling = true;
            SetMagpieInfo(wParam is 1 ? lParam : s_magpieWindowHandle);
        }
    }

    private static void MagpieProcess_Exited(object? sender, EventArgs e)
    {
        Process? magpieProcess = s_magpieProcess;
        IsMagpieScaling = false;
        if (magpieProcess is not null)
        {
            s_magpieProcess = null;
            magpieProcess.Exited -= MagpieProcess_Exited;
            magpieProcess.Dispose();
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
