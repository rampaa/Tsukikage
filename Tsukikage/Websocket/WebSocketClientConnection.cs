using System.Buffers;
using System.Diagnostics;
using System.Net.WebSockets;
using Tsukikage.OCR;

namespace Tsukikage.Websocket;

internal sealed class WebSocketClientConnection : IDisposable
{
    private ClientWebSocket? _webSocketClient;
    private readonly Uri _webSocketUri;

    public WebSocketClientConnection(Uri webSocketUri)
    {
        _webSocketUri = webSocketUri;
    }

    public Task Disconnect()
    {
        return _webSocketClient?.State is WebSocketState.Open
            ? _webSocketClient.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, nameof(WebSocketCloseStatus.NormalClosure), CancellationToken.None)
            : Task.CompletedTask;
    }

    public Task ListenWebSocket(bool textHookerConnection)
    {
        return Task.Run(async () =>
        {
            do
            {
                try
                {
                    using ClientWebSocket webSocketClient = new();
                    await webSocketClient.ConnectAsync(_webSocketUri, CancellationToken.None).ConfigureAwait(false);
                    _webSocketClient = webSocketClient;

                    Console.WriteLine($"Connected to {(textHookerConnection ? "Text Hooker" : "OCR Engine")}");

                    byte[] rentedBuffer = ArrayPool<byte>.Shared.Rent(1024 * 4);
                    try
                    {
                        Memory<byte> buffer = rentedBuffer;
                        while (webSocketClient.State is WebSocketState.Open)
                        {
                            try
                            {
                                ValueWebSocketReceiveResult result = await webSocketClient.ReceiveAsync(buffer, CancellationToken.None).ConfigureAwait(false);
                                if (result.MessageType is WebSocketMessageType.Text)
                                {
                                    int totalBytesReceived = result.Count;
                                    while (!result.EndOfMessage)
                                    {
                                        if (totalBytesReceived == buffer.Length)
                                        {
                                            byte[] newBuffer = ArrayPool<byte>.Shared.Rent(buffer.Length * 2);
                                            buffer.CopyTo(newBuffer);
                                            ArrayPool<byte>.Shared.Return(rentedBuffer);
                                            rentedBuffer = newBuffer;
                                            buffer = rentedBuffer;
                                        }

                                        result = await webSocketClient.ReceiveAsync(buffer[totalBytesReceived..], CancellationToken.None).ConfigureAwait(false);
                                        totalBytesReceived += result.Count;
                                    }

                                    string text = WebsocketServerUtils.Utf8NoBom.GetString(buffer.Span[..totalBytesReceived]);
                                    OcrUtils.ProcessWebSocketText(text, textHookerConnection);
                                }
                                else if (result.MessageType is WebSocketMessageType.Close)
                                {
                                    Debug.WriteLine("WebSocket server is closed");
                                    break;
                                }
                            }
                            catch (WebSocketException webSocketException)
                            {
                                await Console.Error.WriteLineAsync($"WebSocket server is closed unexpectedly\n{webSocketException}").ConfigureAwait(false);
                                break;
                            }
                        }
                    }
                    finally
                    {
                        ArrayPool<byte>.Shared.Return(rentedBuffer);
                    }
                }

                catch (WebSocketException webSocketException)
                {
                    Debug.WriteLine($"Couldn't connect to the WebSocket server, probably because it is not running\n{webSocketException}");
                }

                catch (Exception ex)
                {
                    await Console.Error.WriteLineAsync($"An unexpected error occured while listening the websocket server at {_webSocketUri}\n{ex}").ConfigureAwait(false);
                    return;
                }
            }
            while (true);
        }, CancellationToken.None);
    }

    public void Dispose()
    {
        _webSocketClient?.Dispose();
        _webSocketClient = null;
    }
}
