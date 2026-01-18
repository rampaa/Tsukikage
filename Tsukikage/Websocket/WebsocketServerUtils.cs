using System.Collections.Concurrent;
using System.Text;
using Fleck;

namespace Tsukikage.Websocket;

internal static class WebsocketServerUtils
{
    public static readonly Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: false);

    public static readonly ConcurrentDictionary<IWebSocketConnection, byte> Clients = new();

    public static void InitServer(Uri webSocketServerAddress)
    {
        FleckLog.Level = LogLevel.Info;

        WebSocketServer server = new(webSocketServerAddress.OriginalString);
        server.Start(socket =>
        {
            socket.OnOpen = () =>
            {
                _ = Clients.TryAdd(socket, 0);
                Console.WriteLine($"Client connected ({Clients.Count})");
            };

            socket.OnClose = () =>
            {
                _ = Clients.TryRemove(socket, out _);
                Console.WriteLine($"Client disconnected ({Clients.Count})");
            };

            socket.OnError = ex =>
            {
                _ = Clients.TryRemove(socket, out _);
                Console.Error.WriteLine($"Socket error: {ex.Message}");
            };
        });
    }

    public static void Broadcast(string message)
    {
        foreach (IWebSocketConnection socket in Clients.Keys)
        {
            if (socket.IsAvailable)
            {
                _ = socket.Send(message);
            }
        }
    }
}
