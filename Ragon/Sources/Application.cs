using System;
using System.Threading;
using Ragon.Common;
using NLog;

namespace Ragon.Core
{
  public class Application : IEventHandler
  {
    private readonly RoomManager _roomManager;
    private readonly Thread _thread;
    private readonly Lobby _lobby;
    private readonly ISocketServer _socketServer;
    private readonly Dispatcher _dispatcher;
    private readonly ILogger _logger = LogManager.GetCurrentClassLogger();
    private readonly float _deltaTime = 0.0f;
    private readonly Configuration _configuration;
    private readonly RagonSerializer _serializer;
    
    public ISocketServer SocketServer => _socketServer;
    public Dispatcher Dispatcher => _dispatcher;

    public Application(PluginFactory factory, Configuration configuration)
    {
      var authorizationProvider = factory.CreateAuthorizationProvider(configuration);
      
      _configuration = configuration;
      _serializer = new RagonSerializer();
      
      var dispatcher = new Dispatcher();
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
        _dispatcher.Process();
        _roomManager.Tick(_deltaTime);
        //
        Thread.Sleep((int) _deltaTime);
      }
    }
    
    public void OnConnected(ushort peerId)
    {
        _logger.Trace("Connected " + peerId);
    }

    public void OnDisconnected(ushort peerId)
    {
      _logger.Trace("Disconnected " + peerId);
      
      var player = _lobby.AuthorizationManager.GetPlayer(peerId);
      if (player != null)
        _roomManager.Left(player, Array.Empty<byte>());

      _lobby.OnDisconnected(peerId);
    }

    public void OnData(ushort peerId, byte[] data)
    {
      try
      {
        _serializer.Clear();
        _serializer.FromArray(data);

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

    public void OnTimeout(ushort peerId)
    {
      var player = _lobby.AuthorizationManager.GetPlayer(peerId);
      if (player != null)
        _roomManager.Left(player, Array.Empty<byte>());

      _lobby.OnDisconnected(peerId);
    }
  }
}