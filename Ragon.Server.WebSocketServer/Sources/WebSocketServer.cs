/*
 * Copyright 2023 Eduard Kargin <kargin.eduard@gmail.com>
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System.Net;
using System.Net.WebSockets;
using Ragon.Protocol;
using Ragon.Server.IO;
using Ragon.Server.Logging;

namespace Ragon.Server.WebSocketServer;

public class WebSocketServer : INetworkServer
{
  private readonly IRagonLogger _logger = LoggerManager.GetLogger(nameof(WebSocketServer));
  
  private INetworkListener _networkListener;
  private Stack<ushort> _sequencer;
  private Executor _executor;
  private HttpListener _httpListener;
  private WebSocketConnection[] _connections;
  private List<WebSocketConnection> _activeConnections;
  private CancellationTokenSource _cancellationTokenSource;

  public WebSocketServer()
  {
    _sequencer = new Stack<ushort>();
    _connections = Array.Empty<WebSocketConnection>();
    _activeConnections = new List<WebSocketConnection>();
    _executor = new Executor();
  }
  
  public async void StartAccept(CancellationToken cancellationToken)
  {
    while (!cancellationToken.IsCancellationRequested)
    {
      WebSocketConnection connection = null!;
      try
      {
        var context = await _httpListener.GetContextAsync();
        if (!context.Request.IsWebSocketRequest)
        {
          context.Response.StatusCode = 200;
          context.Response.ContentLength64 = 0;
          context.Response.Close();
          continue;
        }
        
        var webSocketContext = await context.AcceptWebSocketAsync(null);
        var webSocket = webSocketContext.WebSocket;
        var peerId = _sequencer.Pop();
        
        connection = new WebSocketConnection(webSocket, peerId);  
      }
      catch (Exception ex)
      {
          _logger.Error(ex);
          continue;
      }
      
      _connections[connection.Id] = connection;
      
      StartListen(connection, cancellationToken);
    }
  }

  async void StartListen(WebSocketConnection connection, CancellationToken cancellationToken)
  {
    _activeConnections.Add(connection);
    _networkListener.OnConnected(connection);

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
        if (result.Count > 0)
        {
          var payload = buffer.Slice(0, result.Count);
          _networkListener.OnData(connection, NetworkChannel.RELIABLE, payload.ToArray());
        }
      }
      catch (Exception ex)
      {
        break;
      }
    }

    _sequencer.Push(connection.Id);
    _activeConnections.Remove(connection);
    
    _networkListener.OnDisconnected(connection);
  }

  public void Update()
  {
    _executor.Update();
    
    Flush();
  }

  public void Broadcast(byte[] data, NetworkChannel channel)
  {
    foreach (var activeConnection in _activeConnections)
      activeConnection.Reliable.Send(data);    
  }

  public async void Flush()
  {
    foreach (var conn in _activeConnections)
      await conn.Flush();
  }

  public void Listen(
    INetworkListener listener,
    NetworkConfiguration configuration
  )
  {
    _networkListener = listener;
    _cancellationTokenSource = new CancellationTokenSource();

    var limit = (ushort)configuration.LimitConnections;
    for (ushort i = limit; i != 0; i--)
      _sequencer.Push(i);

    _sequencer.Push(0);

    _connections = new WebSocketConnection[configuration.LimitConnections];

    _httpListener = new HttpListener();
    _httpListener.Prefixes.Add($"http://+:{configuration.Port}/");
    _httpListener.Start();

    _executor.Run(() => StartAccept(_cancellationTokenSource.Token));

    var protocolDecoded = RagonVersion.Parse(configuration.Protocol);
    _logger.Info($"Listen at http://0.0.0.0:{configuration.Port}/");
    _logger.Info($"Protocol: {protocolDecoded}");
  }

  public void Stop()
  {
    _cancellationTokenSource.Cancel();
    _httpListener.Stop();
  }
}