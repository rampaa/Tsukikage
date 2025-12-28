using System.Collections.Concurrent;
using System.Text;
using Fleck;

namespace Tsukikage.Websocket;

internal static class WebsocketServerUtils
{
    public static readonly Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: false);

    private static readonly ConcurrentDictionary<IWebSocketConnection, byte> s_clients = new();

    public static void InitServer(Uri webSocketServerAddress)
    {
        FleckLog.Level = LogLevel.Info;

        WebSocketServer server = new(webSocketServerAddress.OriginalString);
        server.Start(socket =>
        {
            socket.OnOpen = () =>
            {
                _ = s_clients.TryAdd(socket, 0);
                Console.WriteLine($"Client connected ({s_clients.Count})");
            };

            socket.OnClose = () =>
            {
                _ = s_clients.TryRemove(socket, out _);
                Console.WriteLine($"Client disconnected ({s_clients.Count})");
            };

            socket.OnError = ex =>
            {
                _ = s_clients.TryRemove(socket, out _);
                Console.Error.WriteLine($"Socket error: {ex.Message}");
            };
        });
    }

    public static void Broadcast(string message)
    {
        foreach (IWebSocketConnection socket in s_clients.Keys)
        {
            if (socket.IsAvailable)
            {
                _ = socket.Send(message);
            }
        }
    }
}
