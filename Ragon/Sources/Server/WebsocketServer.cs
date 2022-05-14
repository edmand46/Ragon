using System;
using System.Net;
using System.Net.WebSockets;
using System.Threading;
using DisruptorUnity3d;
using NLog;

namespace Ragon.Core;

public class WebsocketServer : IDisposable
{
  private HttpListener _httpListener;
  private ILogger _logger = LogManager.GetCurrentClassLogger();
  private Thread _thread;
  private ENet.Event _netEvent;

  private RingBuffer<Event> _receiveBuffer;
  private RingBuffer<Event> _sendBuffer;

  public void WriteEvent(Event evnt) => _sendBuffer.Enqueue(evnt);
  public bool ReadEvent(out Event evnt) => _receiveBuffer.TryDequeue(out evnt);
  
  public void Start(ushort port)
  {
    // _httpListener = new HttpListener();
    // _httpListener.Prefixes.Add("http://localhost/");
    // _httpListener.Start();
    //
    // _thread = new Thread(Execute);
    // _thread.Name = "NetworkThread";
    // _thread.Start();
    // _logger.Info($"Socket Server Started at port {port}");
  }

  public void Execute()
  {
    
  }

  public async void ExecuteAsync()
  {
    // while (true)
    // {
    //   HttpListenerContext context = await _httpListener.GetContextAsync();
    //   if (context.Request.IsWebSocketRequest)
    //   {
    //     HttpListenerWebSocketContext webSocketContext = await context.AcceptWebSocketAsync(null);
    //     WebSocket webSocket = webSocketContext.WebSocket;
    //     while (webSocket.State == WebSocketState.Open)
    //     {
    //       await webSocket.SendAsync(... );
    //     }
    //   }
    // }
  }

  public void Dispose()
  {
  }
}