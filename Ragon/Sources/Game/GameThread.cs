using System;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
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
    private readonly Stopwatch _gameLoopTimer;
    private readonly Stopwatch _statisticsTimer;
    
    private readonly Stopwatch _serverTimer;
    private readonly Stopwatch _logicTimer;
    
    private readonly Lobby _lobby;
    private readonly ILogger _logger = LogManager.GetCurrentClassLogger();
    private readonly float _deltaTime = 0.0f;
    private readonly Configuration _configuration;
    private readonly IDispatcherInternal _dispatcherInternal;

    public IDispatcher ThreadDispatcher { get; private set; }
    public ISocketServer Server { get; private set; }

    public GameThread(PluginFactory factory, Configuration configuration)
    {
      _configuration = configuration;

      var authorizationProvider = factory.CreateAuthorizationProvider(configuration);
      var dispatcher = new Dispatcher();
      _dispatcherInternal = dispatcher;

      ThreadDispatcher = dispatcher;
      Server = new ENetServer(this);

      _deltaTime = 1000.0f / configuration.SendRate;

      _roomManager = new RoomManager(factory, this);
      _lobby = new Lobby(authorizationProvider, _roomManager, this);

      _gameLoopTimer = new Stopwatch();
      _statisticsTimer = new Stopwatch();
      _serverTimer = new Stopwatch();
      _logicTimer = new Stopwatch();

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
      Server.Start(_configuration.Port, _configuration.MaxConnections, encoded);

      _gameLoopTimer.Start();
      _statisticsTimer.Start();
      _logicTimer.Start();
      _serverTimer.Start();
      _thread.Start();
    }

    public void Stop()
    {
      Server.Stop();

      _gameLoopTimer.Stop();
      _statisticsTimer.Stop();
      _thread.Interrupt();
    }

    private void Execute()
    {
      while (true)
      {
        _logicTimer.Restart();
        _serverTimer.Restart();
        
        Server.Process();

        _dispatcherInternal.Process();

        var elapsedMilliseconds = _gameLoopTimer.ElapsedMilliseconds;
        if (elapsedMilliseconds > _deltaTime)
        {
          _roomManager.Tick(elapsedMilliseconds / 1000.0f);
          _gameLoopTimer.Restart();
          continue;
        }

        if (_statisticsTimer.Elapsed.Seconds > _configuration.StatisticsInterval && _roomManager.RoomsBySocket.Count > 0)
        {
          var rooms = _roomManager.Rooms.Count;
          var clients = _roomManager.RoomsBySocket.Count;
          var entities = _roomManager.Rooms.Select(r => r.EntitiesCount).Sum();
          _logger.Trace($"Rooms: {rooms} Clients: {clients} Entities: {entities}");
          _statisticsTimer.Restart();
        }
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