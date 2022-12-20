using System.Net.WebSockets;
using NLog;

namespace Ragon.Server.NativeWebSockets;

public sealed class WebSocketConnection : INetworkConnection
{
    private Logger _logger = LogManager.GetCurrentClassLogger();
    public ushort Id { get; }
    public INetworkChannel Reliable { get; private set; }
    public INetworkChannel Unreliable { get; private set; }

    public WebSocket Socket { get; private set; }
    private WebSocketReliableChannel[] _channels;

    public WebSocketConnection(WebSocket webSocket, ushort peerId)
    {
        Id = peerId;
        Socket = webSocket;

        var reliableChannel = new WebSocketReliableChannel(webSocket);
        var unreliableChannel = new WebSocketReliableChannel(webSocket);

        _channels = new[] { reliableChannel, unreliableChannel };

        Reliable = reliableChannel;
        Unreliable = unreliableChannel;
    }

    public async Task Flush()
    {
        foreach (var channel in _channels)
        {
            try
            {
                await channel.Flush();
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
            }
        }
    }
}