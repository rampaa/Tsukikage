using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text;
using Fleck;

namespace Tsukikage.Websocket;

internal static class WebsocketServerUtils
{
    public static readonly Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: false);

    public static readonly ConcurrentDictionary<IWebSocketConnection, byte> Clients = new();

    public static WebSocketServer? Server { get; set; }

    public static async Task<bool> InitServer(Uri webSocketServerAddress)
    {
        bool isEndpointInUse = await IsEndpointInUse(webSocketServerAddress).ConfigureAwait(false);
        if (isEndpointInUse)
        {
            Console.Error.WriteLine($"WebSocket server couldn't start. The address {webSocketServerAddress.Host}:{webSocketServerAddress.Port} is already in use.");
            return false;
        }

        FleckLog.Level = LogLevel.Info;

        Server = new(webSocketServerAddress.OriginalString)
        {
            RestartAfterListenError = true
        };

        Server.Start(socket =>
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

        return true;
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

    private static async Task<bool> IsEndpointInUse(Uri uri)
    {
        using TcpClient tcpClient = new();

        try
        {
            await tcpClient.ConnectAsync(uri.Host, uri.Port);
            return true;
        }
        catch (SocketException)
        {
            return false;
        }
    }
}
