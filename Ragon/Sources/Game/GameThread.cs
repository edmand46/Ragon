using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using ENet;
using NLog;

namespace Ragon.Core
{
  public class GameThread : IGameThread, IHandler
  {
    private readonly RoomManager _roomManager;
    private readonly Thread _thread;
    private readonly Stopwatch _gameLoopTimer;
    private readonly Lobby _lobby;
    private readonly ILogger _logger = LogManager.GetCurrentClassLogger();
    private readonly float _deltaTime = 0.0f;
    private readonly Stopwatch _statisticsTimer;
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

      _thread = new Thread(Execute);
      _thread.Name = "Game Thread";
      _thread.IsBackground = true;
    }

    public void Start()
    {
      Server.Start(_configuration.Port, _configuration.MaxConnections);

      _gameLoopTimer.Start();
      _statisticsTimer.Start();
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
          _logger.Trace($"Rooms: {_roomManager.Rooms.Count} Clients: {_roomManager.RoomsBySocket.Count}");
          _statisticsTimer.Restart();
        }

        Thread.Sleep(15);
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
          var peerId = evnt.Peer.ID;
          var dataRaw = new byte[evnt.Packet.Length];
          evnt.Packet.CopyTo(dataRaw);

          var data = new ReadOnlySpan<byte>(dataRaw);
          if (_roomManager.RoomsBySocket.TryGetValue(peerId, out var room))
          {
            room.ProcessEvent(peerId, data);
          }
          else
          {
            _lobby.ProcessEvent(peerId, data);
          }
        }
        catch (Exception exception)
        {
          _logger.Error(exception);
        }
      }
    }
  }
}