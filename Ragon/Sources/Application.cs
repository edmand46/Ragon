using System;
using System.Threading;
using Ragon.Common;
using ENet;
using NLog;

namespace Ragon.Core
{
  public class Application : IHandler
  {
    private readonly RoomManager _roomManager;
    private readonly Thread _thread;
    private readonly Lobby _lobby;
    private readonly ISocketServer _socketServer;
    private readonly IDispatcherInternal _dispatcherInternal;
    private readonly IDispatcher _dispatcher;
    private readonly ILogger _logger = LogManager.GetCurrentClassLogger();
    private readonly float _deltaTime = 0.0f;
    private readonly Configuration _configuration;
    private readonly RagonSerializer _serializer;
    
    public ISocketServer SocketServer => _socketServer;
    public IDispatcher Dispatcher => _dispatcher;

    public Application(PluginFactory factory, Configuration configuration)
    {
      var authorizationProvider = factory.CreateAuthorizationProvider(configuration);
      
      _configuration = configuration;
      _serializer = new RagonSerializer();
      
      var dispatcher = new Dispatcher();
      _dispatcherInternal = dispatcher;
      _dispatcher = dispatcher;

      _socketServer = new ENetServer(this);
      _deltaTime = 1000.0f / configuration.SendRate;

      _roomManager = new RoomManager(factory, this);
      _lobby = new Lobby(authorizationProvider, _roomManager, this);

      _thread = new Thread(Execute);
      _thread.Name = "Game Thread";
      _thread.IsBackground = true;
    }

    public void Start()
    {
      var strings = _configuration.Protocol.Split(".");
      if (strings.Length < 3)
      {
        _logger.Error("Wrong protocol passed to connect method");
        return;
      }

      var parts = new uint[] {0, 0, 0};
      for (int i = 0; i < parts.Length; i++)
      {
        if (!uint.TryParse(strings[i], out var v))
        {
          _logger.Error("Wrong protocol");
          return;
        }

        parts[i] = v;
      }

      uint encoded = (parts[0] << 16) | (parts[1] << 8) | parts[2];
      _socketServer.Start(_configuration.Port, _configuration.MaxConnections, encoded);
      _thread.Start();
    }

    public void Stop()
    {
      _socketServer.Stop();
      _thread.Interrupt();
    }

    private void Execute()
    {
      while (true)
      {
        _socketServer.Process();
        _dispatcherInternal.Process();
        _roomManager.Tick(_deltaTime);

        Thread.Sleep((int) _deltaTime);
      }
    }


    public void OnEvent(Event evnt)
    {
      if (evnt.Type == EventType.Timeout || evnt.Type == EventType.Disconnect)
      {
        var player = _lobby.AuthorizationManager.GetPlayer((ushort) evnt.Peer.ID);
        if (player != null)
          _roomManager.Left(player, Array.Empty<byte>());

        _lobby.OnDisconnected((ushort) evnt.Peer.ID);
      }

      if (evnt.Type == EventType.Receive)
      {
        try
        {
          var peerId = (ushort) evnt.Peer.ID;
          var dataRaw = new byte[evnt.Packet.Length];
          evnt.Packet.CopyTo(dataRaw);
          _serializer.FromArray(dataRaw);

          var operation = _serializer.ReadOperation();
          if (_roomManager.RoomsBySocket.TryGetValue(peerId, out var room))
            room.ProcessEvent(peerId, operation, _serializer);

          _lobby.ProcessEvent(peerId, operation, _serializer);
        }
        catch (Exception exception)
        {
          _logger.Error(exception);
        }
      }
    }
  }
}