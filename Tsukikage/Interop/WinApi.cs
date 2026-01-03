using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Tsukikage.OCR;
using static Tsukikage.Interop.WinApi.NativeMethods;

namespace Tsukikage.Interop;

internal static partial class WinApi
{
    private static readonly bool s_is64BitProcess = Environment.Is64BitProcess;
    private static readonly WndProc s_wndProc = WindowProc; //

#pragma warning disable IDE1006 // Naming rule violation
    internal static partial class NativeMethods
    {
        // ReSharper disable InconsistentNaming
        internal const uint WS_POPUP = 0x80000000;
        internal const uint WS_EX_TOOLWINDOW = 0x00000080;
        internal const uint WS_EX_NOACTIVATE = 0x08000000;
        internal const uint CF_UNICODETEXT = 13;
        internal const uint GMEM_MOVEABLE = 0x0002;
        internal const int WM_INPUT = 0x00FF;
        internal const int RIDEV_INPUTSINK = 0x00000100;

        internal delegate nint LowLevelMouseProc(int nCode, nint wParam, nint lParam);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        internal delegate nint WndProc(nint hWnd, uint msg, nint wParam, nint lParam);

        [StructLayout(LayoutKind.Sequential)]
        internal struct RAWINPUTDEVICE
        {
            public ushort usUsagePage;
            public ushort usUsage;
            public int dwFlags;
            public nint hwndTarget;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct MSG
        {
            public nint hwnd;
            public uint message;
            public nint wParam;
            public nint lParam;
            public uint time;
            public Point pt;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct WNDCLASS
        {
            public uint style;
            public nint lpfnWndProc;
            public int cbClsExtra;
            public int cbWndExtra;
            public nint hInstance;
            public nint hIcon;
            public nint hCursor;
            public nint hbrBackground;
            public string? lpszMenuName;
            public string lpszClassName;
        }

        // ReSharper restore InconsistentNaming

        // ReSharper disable UnusedMember.Global
        internal enum ChangeWindowMessageFilterExAction
        {
            Reset = 0,
            Allow = 1,
            Disallow = 2
        }
        // ReSharper restore UnusedMember.Global

        private const string User32 = "user32.dll";
        private const string Kernel32 = "kernel32.dll";

        [LibraryImport(User32, SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool OpenClipboard(nint hWndNewOwner);

        [LibraryImport(User32, SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool CloseClipboard();

        [LibraryImport("user32.dll", EntryPoint = "EmptyClipboard", SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool EmptyClipboard();

        [LibraryImport("user32.dll", EntryPoint = "SetClipboardData", SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        internal static partial nint SetClipboardData(uint uFormat, nint hMem);

        [LibraryImport("kernel32.dll", EntryPoint = "GlobalAlloc", SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        internal static partial nint GlobalAlloc(uint uFlags, nuint dwBytes);

        [LibraryImport("kernel32.dll", EntryPoint = "GlobalFree", SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        internal static partial nint GlobalFree(nint hMem);

        [LibraryImport(User32, EntryPoint = "IsClipboardFormatAvailable")]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool IsClipboardFormatAvailable(uint format);

        [LibraryImport(User32, SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        internal static partial nint GetClipboardData(uint uFormat);

        [LibraryImport(Kernel32, SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        internal static partial nint GlobalLock(nint hMem);

        [LibraryImport(Kernel32, SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool GlobalUnlock(nint hMem);

        [LibraryImport("user32.dll", SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool RegisterRawInputDevices([In] RAWINPUTDEVICE[] pRawInputDevices, int uiNumDevices, int cbSize);

        [LibraryImport("user32.dll", SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        internal static partial uint GetRawInputData(nint hRawInput, uint uiCommand, nint pData, ref uint pcbSize, uint cbSizeHeader);

        [LibraryImport("user32.dll", SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool GetCursorPos(out Point lpPoint);

        [LibraryImport("user32.dll", EntryPoint = "RegisterWindowMessageW", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        internal static partial int RegisterWindowMessage(string lpString);

        [LibraryImport("user32.dll", EntryPoint = "ChangeWindowMessageFilterEx", SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool ChangeWindowMessageFilterEx(nint hWnd, int msg, ChangeWindowMessageFilterExAction action, nint changeInfo);

        [LibraryImport("user32.dll", EntryPoint = "GetPropW", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        public static partial nint GetPropW(nint hWnd, string lpString);

        [LibraryImport("user32.dll", EntryPoint = "FindWindowW", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        public static partial nint FindWindowW(string? lpClassName, string? lpWindowName);

        [LibraryImport("user32.dll", EntryPoint = "GetWindowThreadProcessId", SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        public static partial uint GetWindowThreadProcessId(nint hWnd, out uint lpdwProcessId);

        [LibraryImport("user32.dll", EntryPoint = "WindowFromPoint", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        public static partial nint WindowFromPoint(Point point);

        [LibraryImport("user32.dll", EntryPoint = "AddClipboardFormatListener", SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool AddClipboardFormatListener(nint hwnd);

        [LibraryImport("user32.dll", EntryPoint = "RemoveClipboardFormatListener", SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool RemoveClipboardFormatListener(nint hwnd);

        [LibraryImport("user32.dll", EntryPoint = "GetClipboardSequenceNumber")]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        internal static partial ulong GetClipboardSequenceNumber();

        [LibraryImport("user32.dll", EntryPoint = "SetWindowLongW", SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        private static partial int SetWindowLong32(nint hWnd, int nIndex, int dwNewLong);

        [LibraryImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        private static partial nint SetWindowLongPtr64(nint hWnd, int nIndex, nint dwNewLong);

        [LibraryImport("user32.dll", EntryPoint = "SetWindowsHookExW", SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        internal static partial nint SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, nint hMod, uint dwThreadId);

        [DllImport("user32.dll", EntryPoint = "RegisterClassW", SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
#pragma warning disable SYSLIB1054 // Use 'LibraryImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time
        internal static extern ushort RegisterClass(ref WNDCLASS lpWndClass);
#pragma warning restore SYSLIB1054 // Use 'LibraryImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time

        [LibraryImport("user32.dll", EntryPoint = "CreateWindowExW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        internal static partial nint CreateWindowEx(uint dwExStyle, string lpClassName, string? lpWindowName, uint dwStyle, int x, int y, int nWidth, int nHeight, nint hWndParent, nint hMenu, nint hInstance, nint lpParam);

        [LibraryImport("user32.dll", EntryPoint = "GetMessageW", SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        internal static partial int GetMessage(out MSG lpMsg, nint hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

        [LibraryImport("user32.dll", EntryPoint = "DispatchMessageW")]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        internal static partial nint DispatchMessage(ref MSG lpMsg);

        [LibraryImport("user32.dll")]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool TranslateMessage(ref MSG lpMsg);

        [LibraryImport("user32.dll", EntryPoint = "DefWindowProcW")]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        internal static partial nint DefWindowProc(nint hWnd, uint msg, nint wParam, nint lParam);

        [LibraryImport("kernel32.dll", EntryPoint = "GetModuleHandleW", StringMarshalling = StringMarshalling.Utf16)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        internal static partial nint GetModuleHandle(string? lpModuleName);

        [LibraryImport("user32.dll", EntryPoint = "CallNextHookEx", SetLastError = true)]
#pragma warning disable CA1711 // Identifiers should not have incorrect suffix
        internal static partial nint CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
#pragma warning restore CA1711 // Identifiers should not have incorrect suffix

        internal static nint SetWindowLongPtr(nint hWnd, int nIndex, nint dwNewLong)
        {
            return s_is64BitProcess
                ? SetWindowLongPtr64(hWnd, nIndex, dwNewLong)
                : SetWindowLong32(hWnd, nIndex, (int)dwNewLong);
        }

        [LibraryImport("user32.dll", EntryPoint = "GetWindowLongW", SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        private static partial int GetWindowLongPtr32(nint hWnd, int nIndex);

        [LibraryImport("user32.dll", EntryPoint = "GetWindowLongPtrW", SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        private static partial nint GetWindowLongPtr64(nint hWnd, int nIndex);

        internal static nint GetWindowLongPtr(nint hWnd, int nIndex)
        {
            return s_is64BitProcess
                ? GetWindowLongPtr64(hWnd, nIndex)
                : GetWindowLongPtr32(hWnd, nIndex);
        }
    }
#pragma warning restore IDE1006 // Naming rule violation

    public static nint CreateHiddenWindow()
    {
        WNDCLASS wc = new()
        {
            lpfnWndProc = Marshal.GetFunctionPointerForDelegate(s_wndProc),
            hInstance = GetModuleHandle(null),
            lpszClassName = "RawInputSinkWindow"
        };

        ushort atom = RegisterClass(ref wc);
        if (atom == 0)
        {
            int err = Marshal.GetLastWin32Error();
            throw new Win32Exception(err);
        }

        nint hwnd = CreateWindowEx(
            WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE,
            wc.lpszClassName,
            null,
            WS_POPUP,
            0, 0, 0, 0,
            0,
            0,
            wc.hInstance,
            0);

        if (hwnd is not 0)
        {
            return hwnd;
        }

        int error = Marshal.GetLastWin32Error();
        throw new Win32Exception(error);
    }


    public static int RegisterToWindowMessage(string messageName)
    {
        return RegisterWindowMessage(messageName);
    }

    private static bool ChangeWindowMessageFilter(nint windowHandle, int message, ChangeWindowMessageFilterExAction filterAction)
    {
        return ChangeWindowMessageFilterEx(windowHandle, message, filterAction, 0);
    }

    public static bool AllowWindowMessage(nint windowHandle, int message)
    {
        return ChangeWindowMessageFilter(windowHandle, message, ChangeWindowMessageFilterExAction.Allow);
    }

    public static nint GetProp(nint windowHandle, string lpString)
    {
        return GetPropW(windowHandle, lpString);
    }

    public static nint FindWindow(string lpClassName)
    {
        return FindWindowW(lpClassName, null);
    }

    public static nint GetWindowHandleFromPoint(Point point)
    {
        return WindowFromPoint(point);
    }

    private static nint WindowProc(nint hWnd, uint msg, nint wParam, nint lParam)
    {
        if (msg is WM_INPUT)
        {
            if (GetCursorPos(out Point cursorPosition))
            {
                MouseMoveWorker.Signal(cursorPosition);
            }
        }
        else if (msg == MagpieUtils.MagpieScalingChangedWindowMessage)
        {
            MagpieUtils.SetMagpieInfo(wParam, lParam);
        }

        return DefWindowProc(hWnd, msg, wParam, lParam);
    }

    public static void RegisterForRawMouseInput(nint hwnd)
    {
        RAWINPUTDEVICE[] devices =
        [
            new()
            {
                usUsagePage = 0x01, // Generic desktop controls
                usUsage     = 0x02, // Mouse
                dwFlags     = RIDEV_INPUTSINK,
                hwndTarget  = hwnd
            }
        ];

        if (!RegisterRawInputDevices(devices, devices.Length, Marshal.SizeOf<RAWINPUTDEVICE>()))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }
    }

    public static void SetClipboardText(string text)
    {
        nuint byteCount = (nuint)((text.Length + 1) * sizeof(char));
        char[] chars = text.ToCharArray();

        const int sleepDuration = 10;
        while (true)
        {
            nint hMem = GlobalAlloc(GMEM_MOVEABLE, byteCount);
            if (hMem is 0)
            {
                Thread.Sleep(sleepDuration);
                continue;
            }

            try
            {
                nint ptr = GlobalLock(hMem);
                if (ptr is 0)
                {
                    Thread.Sleep(sleepDuration);
                    continue;
                }

                Marshal.Copy(chars, 0, ptr, chars.Length);
                Marshal.WriteInt16(ptr, chars.Length * sizeof(char), 0);

                _ = GlobalUnlock(hMem);
                if (Marshal.GetLastWin32Error() is not 0 || !OpenClipboard(0))
                {
                    Thread.Sleep(sleepDuration);
                    continue;
                }

                try
                {
                    if (!EmptyClipboard())
                    {
                        continue;
                    }

                    if (SetClipboardData(CF_UNICODETEXT, hMem) is 0)
                    {
                        continue;
                    }

                    hMem = 0;
                    return;
                }
                finally
                {
                    _ = CloseClipboard();
                }
            }
            finally
            {
                if (hMem is not 0)
                {
                    _ = GlobalFree(hMem);
                }
            }
        }
    }

    public static Process? GetProcessByWindowClassName(string windowClassName)
    {
        nint windowHandle = FindWindow(windowClassName);
        return windowHandle is not 0
            ? GetProcessByWindowHandle(windowHandle)
            : null;
    }

    public static Process? GetProcessByWindowHandle(nint windowHandle)
    {
        uint threadId = GetWindowThreadProcessId(windowHandle, out uint pid);
        if (threadId is not 0)
        {
            try
            {
                Process process = Process.GetProcessById((int)pid);
                process.EnableRaisingEvents = true;
                return process;
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        return null;
    }

    public static void RunMessageLoop()
    {
        while (GetMessage(out MSG msg, 0, 0, 0) > 0)
        {
            _ = TranslateMessage(ref msg);
            _ = DispatchMessage(ref msg);
        }
    }
}
