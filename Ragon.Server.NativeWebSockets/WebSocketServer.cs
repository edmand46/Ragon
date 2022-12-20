using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.WebSockets;
using NLog;
using Ragon.Common;
using Ragon.Core.Server;
using Ragon.Server;
using Ragon.Server.NativeWebSockets;

namespace Ragon.Core;

public class NativeWebSocketServer : INetworkServer
{
    private ILogger _logger = LogManager.GetCurrentClassLogger();
    private INetworkListener _networkListener;
    private Stack<ushort> _sequencer;
    private Executor _executor;
    private HttpListener _httpListener;
    private WebSocketConnection[] _connections;
    private ushort _lastPeerId;
    private CancellationTokenSource _cancellationTokenSource;

    public NativeWebSocketServer(Executor executor)
    {
        _sequencer = new Stack<ushort>();
        _connections = Array.Empty<WebSocketConnection>();
        _executor = executor;
    }

    public async Task StartAccept(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var context = await _httpListener.GetContextAsync();
            if (!context.Request.IsWebSocketRequest) continue;

            var webSocketContext = await context.AcceptWebSocketAsync(null);
            var webSocket = webSocketContext.WebSocket;

            var peerId = _sequencer.Pop();
            var connection = new WebSocketConnection(webSocket, peerId);

            _lastPeerId = peerId;
            _connections[peerId] = connection;
            _networkListener.OnConnected(connection);
            _executor.Run(StartListen(connection, cancellationToken));
        }
    }

    async Task StartListen(WebSocketConnection connection, CancellationToken cancellationToken)
    {
        var webSocket = connection.Socket;
        var bytes = new byte[2048];
        var buffer = new Memory<byte>(bytes);
        while (
            webSocket.State == WebSocketState.Open ||
            !cancellationToken.IsCancellationRequested)
        {
            try
            {
                var result = await webSocket.ReceiveAsync(buffer, cancellationToken);
                var dataRaw = buffer.Slice(0, result.Count);

                _networkListener.OnData(connection, dataRaw.ToArray());
            }
            catch (Exception ex)
            {
                break;
            }
        }
        
        _sequencer.Push(connection.Id);
        _networkListener.OnDisconnected(connection);
    }

    public async void Poll()
    {
        foreach (var conn in _connections)
        {
            if (conn != null)
            {
                await conn.Flush();
            }
        }
    }

    public void Start(
        INetworkListener listener,
        NetworkConfiguration configuration
    )
    {
        _networkListener = listener;
        _cancellationTokenSource = new CancellationTokenSource();

        var limit = (ushort) configuration.LimitConnections;
        for (ushort i = limit; i != 0; i--)
            _sequencer.Push(i);
        
        _sequencer.Push(0);

        _connections = new WebSocketConnection[configuration.LimitConnections];
        
        _httpListener = new HttpListener();
        _httpListener.Prefixes.Add($"http://127.0.0.1:{configuration.Port}/");
        _httpListener.Start();

        _executor.Run(StartAccept(_cancellationTokenSource.Token));

        var protocolDecoded = RagonVersion.Parse(configuration.Protocol);
        _logger.Info($"Network listening on http://*:{configuration.Port}/");
        _logger.Info($"Protocol: {protocolDecoded}");
    }

    public void Stop()
    {
        _cancellationTokenSource.Cancel();
        _httpListener.Stop();
    }
}