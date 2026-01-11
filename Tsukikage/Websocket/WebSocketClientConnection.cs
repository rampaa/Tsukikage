using System.Buffers;
using System.Diagnostics;
using System.Net.WebSockets;
using Tsukikage.OCR;
using Tsukikage.Utilities;

namespace Tsukikage.Websocket;

internal sealed class WebSocketClientConnection : IDisposable
{
    private ClientWebSocket? _webSocketClient;
    private CancellationTokenSource? _webSocketCancellationTokenSource;
    private readonly Uri _webSocketUri;

    public WebSocketClientConnection(Uri webSocketUri)
    {
        _webSocketUri = webSocketUri;
    }

    public async Task Disconnect()
    {
        if (_webSocketClient?.State is WebSocketState.Open)
        {
            await _webSocketClient.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, nameof(WebSocketCloseStatus.NormalClosure), CancellationToken.None).ConfigureAwait(false);
        }

        if (_webSocketCancellationTokenSource is null)
        {
            return;
        }

        await _webSocketCancellationTokenSource.CancelAsync().ConfigureAwait(false);
        _webSocketCancellationTokenSource.Dispose();
        _webSocketClient?.Dispose();
        _webSocketCancellationTokenSource = null;
        _webSocketClient = null;
    }

    public void Connect(bool textHookerConnection)
    {
        if (_webSocketCancellationTokenSource is null)
        {
            _webSocketCancellationTokenSource = new CancellationTokenSource();
            ListenWebSocket(textHookerConnection, _webSocketCancellationTokenSource.Token).SafeFireAndForget("Unexpected error while listening the WebSocket");
        }
    }

    public Task ListenWebSocket(bool textHookerConnection, CancellationToken cancellationToken)
    {
        return Task.Run(async () =>
        {
            do
            {
                try
                {
                    using ClientWebSocket webSocketClient = new();
                    await webSocketClient.ConnectAsync(_webSocketUri, cancellationToken).ConfigureAwait(false);
                    _webSocketClient = webSocketClient;

                    Console.WriteLine($"Connected to {(textHookerConnection ? "Text Hooker" : "OCR Engine")}");

                    byte[] rentedBuffer = ArrayPool<byte>.Shared.Rent(1024 * 4);
                    try
                    {
                        Memory<byte> buffer = rentedBuffer;
                        while (!cancellationToken.IsCancellationRequested && webSocketClient.State is WebSocketState.Open)
                        {
                            try
                            {
                                ValueWebSocketReceiveResult result = await webSocketClient.ReceiveAsync(buffer, cancellationToken).ConfigureAwait(false);
                                if (cancellationToken.IsCancellationRequested)
                                {
                                    if (webSocketClient.State is WebSocketState.Open)
                                    {
                                        await webSocketClient.CloseAsync(WebSocketCloseStatus.NormalClosure, nameof(WebSocketCloseStatus.NormalClosure), CancellationToken.None).ConfigureAwait(false);
                                    }

                                    return;
                                }

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

                catch (OperationCanceledException)
                {
                    await Console.Error.WriteLineAsync("WebSocket connection was cancelled.").ConfigureAwait(false);
                    return;
                }

                catch (Exception ex)
                {
                    await Console.Error.WriteLineAsync($"An unexpected error occured while listening the websocket server at {_webSocketUri}\n{ex}").ConfigureAwait(false);
                    return;
                }
            }
            while (!cancellationToken.IsCancellationRequested);
        }, CancellationToken.None);
    }

    public void Dispose()
    {
        _webSocketClient?.Dispose();
        _webSocketClient = null;
    }
}
