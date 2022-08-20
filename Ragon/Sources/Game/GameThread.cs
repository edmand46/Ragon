using System;
using System.Threading;
using Ragon.Common;
using ENet;
using NLog;

namespace Ragon.Core
{
  public class GameThread : IGameThread, IHandler
  {
    private readonly RoomManager _roomManager;
    private readonly Thread _thread;
    private readonly Lobby _lobby;
    private readonly ISocketServer _server;
    private readonly IDispatcherInternal _dispatcherInternal;
    private readonly IDispatcher _dispatcher;
    private readonly ILogger _logger = LogManager.GetCurrentClassLogger();
    private readonly float _deltaTime = 0.0f;
    private readonly Configuration _configuration;

    public ISocketServer Server => _server;
    public IDispatcher ThreadDispatcher => _dispatcher;

    public GameThread(PluginFactory factory, Configuration configuration)
    {
      var authorizationProvider = factory.CreateAuthorizationProvider(configuration);

      _configuration = configuration;

      var dispatcher = new Dispatcher();
      _dispatcherInternal = dispatcher;
      _dispatcher = dispatcher;

      _server = new ENetServer(this);
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
      _server.Start(_configuration.Port, _configuration.MaxConnections, encoded);
      _thread.Start();
    }

    public void Stop()
    {
      _server.Stop();
      _thread.Interrupt();
    }

    private void Execute()
    {
      while (true)
      {
        _server.Process();
        _dispatcherInternal.Process();
        _roomManager.Tick(_deltaTime);

        Thread.Sleep((int) _deltaTime);
      }
    }


    public void OnEvent(Event evnt)
    {
      if (evnt.Type == EventType.Timeout || evnt.Type == EventType.Disconnect)
      {
        var player = _lobby.AuthorizationManager.GetPlayer(evnt.Peer.ID);
        if (player != null)
          _roomManager.Left(player, Array.Empty<byte>());

        _lobby.OnDisconnected(evnt.Peer.ID);
      }

      if (evnt.Type == EventType.Receive)
      {
        try
        {
          var peerId = (ushort) evnt.Peer.ID;
          var dataRaw = new byte[evnt.Packet.Length];
          evnt.Packet.CopyTo(dataRaw);

          var data = new ReadOnlySpan<byte>(dataRaw);
          var operation = (RagonOperation) data[0];
          var payload = data.Slice(1, data.Length - 1);

          if (_roomManager.RoomsBySocket.TryGetValue(peerId, out var room))
            room.ProcessEvent(peerId, operation, payload);

          _lobby.ProcessEvent(peerId, operation, payload);
        }
        catch (Exception exception)
        {
          _logger.Error(exception);
        }
      }
    }
  }
}