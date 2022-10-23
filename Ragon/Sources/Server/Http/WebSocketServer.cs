using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using Ragon.Common;

namespace Ragon.Core;

public class WebSocketServer : ISocketServer
{
  private ushort _idSequencer = 0;
  private ILogger _logger = LogManager.GetCurrentClassLogger();
  private Dictionary<ushort, WebSocket> _webSockets = new Dictionary<ushort, WebSocket>();
  private Queue<WebSocketPacket> _events;
  private IEventHandler _eventHandler;
  private WebSocketTaskScheduler _webSocketScheduler;
  private TaskFactory _taskFactory;
  private HttpListener _httpListener;
  
  public WebSocketServer(IEventHandler eventHandler)
  {
    _eventHandler = eventHandler;
    _events = new Queue<WebSocketPacket>(1024);
    _webSocketScheduler = new WebSocketTaskScheduler();
    _taskFactory = new TaskFactory(_webSocketScheduler);
  }

  async void StartAccept()
  {
    while (true)
    {
      var context = await _httpListener.GetContextAsync();
      if (!context.Request.IsWebSocketRequest) continue;

      var webSocketContext = await context.AcceptWebSocketAsync(null);
      var webSocket = webSocketContext.WebSocket;

      _idSequencer++;
      _webSockets.Add(_idSequencer, webSocket);

      _ = _taskFactory.StartNew(() => StartListen(webSocket, _idSequencer));
    }
  }

  async void StartListen(WebSocket webSocket, ushort peerId)
  {
    _eventHandler.OnConnected(peerId);

    var bytes = new byte[2048];
    var buffer = new Memory<byte>(bytes);
    while (webSocket.State == WebSocketState.Open)
    {
      try
      {
        var result = await webSocket.ReceiveAsync(buffer, CancellationToken.None);
        var dataRaw = buffer.Slice(0, result.Count);
        _eventHandler.OnData(peerId, dataRaw.ToArray());
      }
      catch (Exception ex)
      {
        break;
      }
    }

    _eventHandler.OnDisconnected(peerId);
  }

  async void ProcessQueue()
  {
    while (_events.TryDequeue(out var evnt))
    {
      if (_webSockets.TryGetValue(evnt.PeerId, out var ws) && ws.State == WebSocketState.Open)
      {
        await ws.SendAsync(evnt.Data, WebSocketMessageType.Binary, WebSocketMessageFlags.EndOfMessage, CancellationToken.None);
      }
    }
  }

  public void Start(ushort port, int connections, uint protocol)
  {
    _httpListener = new HttpListener();
    _httpListener.Prefixes.Add($"http://*:{port}/");
    _httpListener.Start();

    _taskFactory.StartNew(StartAccept);

    var protocolDecoded = (protocol >> 16 & 0xFF) + "." + (protocol >> 8 & 0xFF) + "." + (protocol & 0xFF);
    _logger.Info($"Network listening on http://*:{port}/");
    _logger.Info($"Protocol: {protocolDecoded}");
  }

  public void Process()
  {
    _webSocketScheduler.Process();
    
    ProcessQueue();
  }

  public void Stop()
  {
    _httpListener.Stop();
  }

  public void Send(ushort peerId, byte[] data, DeliveryType type)
  {
    _events.Enqueue(new WebSocketPacket() {PeerId = peerId, Data = data});
  }

  public void Broadcast(ushort[] peersIds, byte[] data, DeliveryType type)
  {
    foreach (var peerId in peersIds)
      _events.Enqueue(new WebSocketPacket() {PeerId = peerId, Data = data});
  }

  public void Disconnect(ushort peerId, uint errorCode)
  {
    _webSockets[peerId].CloseAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);
  }
}