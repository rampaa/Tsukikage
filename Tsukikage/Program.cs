using System.Runtime;
using System.Runtime.InteropServices;
using System.Text;
using Tsukikage.Interop;
using Tsukikage.Network;
using Tsukikage.OCR;
using Tsukikage.Utilities.Bool;
using Tsukikage.Websocket;

namespace Tsukikage;

internal static class Program
{
    private static nint WindowHandle { get; set; }
    private static WebSocketClientConnection? s_webSocketClientConnection;
    private static WebSocketClientConnection? s_textHookerWebSocketConnection;
    private static readonly AtomicBool s_cleanupStarted = new(false);

    public static async Task Main()
    {
        Console.InputEncoding = Encoding.Unicode;
        Console.OutputEncoding = Encoding.Unicode;

        Environment.CurrentDirectory = AppContext.BaseDirectory;

        AppDomain.CurrentDomain.UnhandledException += LogUnhandledException;
        TaskScheduler.UnobservedTaskException += LogUnobservedTaskException;

        ProfileOptimization.SetProfileRoot(AppContext.BaseDirectory);
        ProfileOptimization.StartProfile("Startup.Profile");

        using PosixSignalRegistration termSignalRegistration = PosixSignalRegistration.Create(PosixSignal.SIGTERM, static _ => HandleAppExit());
        using PosixSignalRegistration sigHupSignalRegistration = PosixSignalRegistration.Create(PosixSignal.SIGHUP, static _ => HandleAppExit());
        Console.CancelKeyPress += Console_CancelKeyPress;
        AppDomain.CurrentDomain.ProcessExit += Console_AppExit;

        ConfigManager.Load();
        if (ConfigManager.AutoUpdateOnStartup)
        {
            await NetworkUtils.CheckAndInstallTsukikageUpdates().ConfigureAwait(false);
        }

        WindowHandle = WinApi.CreateHiddenWindow();
        WinApi.RegisterForRawMouseInput(WindowHandle);
        MagpieUtils.RegisterToMagpieScalingChangedMessage(WindowHandle);
        MagpieUtils.Init();

        Console.WriteLine(ConfigManager.CurrentConfigString());

        s_webSocketClientConnection = new WebSocketClientConnection(ConfigManager.OcrJsonInputWebSocketAddress);
        s_webSocketClientConnection.Connect(false);

        if (ConfigManager.TextHookerWebSocketAddress is not null)
        {
            s_textHookerWebSocketConnection = new WebSocketClientConnection(ConfigManager.TextHookerWebSocketAddress);
            s_textHookerWebSocketConnection.Connect(true);
        }

        if (ConfigManager.OutputIpcMethod is OutputIpcMethod.WebSocket)
        {
            bool webSocketServerStarted = await WebsocketServerUtils.InitServer(ConfigManager.OutputWebSocketAddress).ConfigureAwait(false);
            if (!webSocketServerStarted)
            {
                return;
            }
        }

        WinApi.RunMessageLoop();
    }

    private static void LogUnhandledException(object sender, UnhandledExceptionEventArgs args)
    {
        Console.Error.WriteLine("Unhandled exception:");
        Console.Error.WriteLine((Exception)args.ExceptionObject);
    }

    private static void LogUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs args)
    {
        Console.Error.WriteLine("Unobserved task exception:");
        Console.Error.WriteLine(args.Exception);
    }

    private static void Console_CancelKeyPress(object? sender, ConsoleCancelEventArgs e)
    {
        e.Cancel = true;
        HandleAppExit();
        WinApi.PostQuitMessage();
    }

    private static void Console_AppExit(object? sender, EventArgs e)
    {
        HandleAppExit();
    }

    private static void HandleAppExit()
    {
        Cleanup().GetAwaiter().GetResult();
    }

    public static async Task Cleanup()
    {
        if (!s_cleanupStarted.TrySetTrue())
        {
            return;
        }

        if (s_webSocketClientConnection is not null)
        {
            await s_webSocketClientConnection.Disconnect().ConfigureAwait(false);
            s_webSocketClientConnection = null;
        }

        if (s_textHookerWebSocketConnection is not null)
        {
            await s_textHookerWebSocketConnection.Disconnect().ConfigureAwait(false);
            s_textHookerWebSocketConnection = null;
        }

        WebsocketServerUtils.Server?.Dispose();
        WebsocketServerUtils.Server = null;
    }
}
