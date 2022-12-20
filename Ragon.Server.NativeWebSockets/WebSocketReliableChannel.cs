using System.Net.WebSockets;
using Ragon.Server;

namespace Ragon.Server.NativeWebSockets;

public class WebSocketReliableChannel : INetworkChannel
{
    private Queue<byte[]> _queue;
    private WebSocket _socket;

    public WebSocketReliableChannel(WebSocket webSocket)
    {
        _socket = webSocket;
        _queue = new Queue<byte[]>(512);
    }

    public void Send(byte[] data)
    {
        _queue.Enqueue(data);
    }

    public async Task Flush()
    {
        while (_queue.TryDequeue(out var sendData) && _socket.State == WebSocketState.Open)
            await _socket.SendAsync(sendData, WebSocketMessageType.Binary, WebSocketMessageFlags.EndOfMessage, CancellationToken.None);
    }
}