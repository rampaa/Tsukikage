using System.Runtime;
using System.Text;
using Tsukikage.Interop;
using Tsukikage.OCR;
using Tsukikage.Utilities;
using Tsukikage.Websocket;

namespace Tsukikage;

file static class Program
{
    private static nint WindowHandle { get; set; }
    private static WebSocketClientConnection? s_webSocketClientConnection;
    private static WebSocketClientConnection? s_textHookerWebSocketConnection;

    public static void Main()
    {
        Console.InputEncoding = Encoding.Unicode;
        Console.OutputEncoding = Encoding.Unicode;

        Environment.CurrentDirectory = AppContext.BaseDirectory;

        AppDomain.CurrentDomain.UnhandledException += LogUnhandledException;
        TaskScheduler.UnobservedTaskException += LogUnobservedTaskException;

        ProfileOptimization.SetProfileRoot(AppContext.BaseDirectory);
        ProfileOptimization.StartProfile("Startup.Profile");

        WindowHandle = WinApi.CreateHiddenWindow();
        WinApi.RegisterForRawMouseInput(WindowHandle);
        MagpieUtils.RegisterToMagpieScalingChangedMessage(WindowHandle);
        MagpieUtils.Init();

        ConfigManager.Load();
        Console.WriteLine(ConfigManager.CurrentConfigString());

        s_webSocketClientConnection = new WebSocketClientConnection(ConfigManager.OcrJsonInputWebSocketAddress);
        s_webSocketClientConnection.ListenWebSocket(false).SafeFireAndForget("ListenWebSocket failed unexpectedly for WebSocket");

        if (ConfigManager.TextHookerWebSocketAddress is not null)
        {
            s_textHookerWebSocketConnection = new WebSocketClientConnection(ConfigManager.TextHookerWebSocketAddress);
            s_textHookerWebSocketConnection.ListenWebSocket(true).SafeFireAndForget("ListenWebSocket failed unexpectedly for TextHookerWebSocket");
        }

        Console.CancelKeyPress += Console_CancelKeyPress;
        AppDomain.CurrentDomain.ProcessExit += Console_CancelKeyPress;

        if (ConfigManager.OutputIpcMethod is OutputIpcMethod.WebSocket)
        {
            WebsocketServerUtils.InitServer(ConfigManager.OutputWebSocketAddress);
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

    private static void Console_CancelKeyPress(object? sender, EventArgs e)
    {
        Cleanup().Wait();
        Environment.Exit(0);
    }

    private static void Console_CancelKeyPress(object? sender, ConsoleCancelEventArgs e)
    {
        e.Cancel = true;
        Cleanup().Wait();
        Environment.Exit(0);
    }

    private static async Task Cleanup()
    {
        if (s_webSocketClientConnection is not null)
        {
            await s_webSocketClientConnection.Disconnect().ConfigureAwait(false);
        }

        if (s_textHookerWebSocketConnection is not null)
        {
            await s_textHookerWebSocketConnection.Disconnect().ConfigureAwait(false);
        }
    }
}
