using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using NLog;

namespace Ragon.Core
{
  public class GameThread : IGameThread
  {
    private readonly Dictionary<uint, GameRoom> _socketByRooms;
    private readonly RoomManager _roomManager;
    private readonly ENetServer _socketServer;
    private readonly Thread _thread;
    private readonly Server _serverConfiguration;
    private readonly Stopwatch _gameLoopTimer;
    private readonly Lobby _lobby;
    private readonly IDispatcher _dispatcher;
    private readonly ILogger _logger = LogManager.GetCurrentClassLogger();
    private readonly float _deltaTime = 0.0f;
    
    private int _packets = 0;
    private readonly Stopwatch _packetsTimer;
    public GameThread(PluginFactory factory, Configuration configuration)
    {
      var authorizationProvider = factory.CreateAuthorizationProvider(configuration);

      _serverConfiguration = configuration.Server;
      _deltaTime = 1000.0f / configuration.TickRate;

      _dispatcher = new Dispatcher();
      _roomManager = new RoomManager(factory, this);
      _lobby = new Lobby(authorizationProvider, _roomManager, this);
      _socketServer = new ENetServer();
      _gameLoopTimer = new Stopwatch();
      _packetsTimer = new Stopwatch();
      _socketByRooms = new Dictionary<uint, GameRoom>();

      _thread = new Thread(Execute);
      _thread.Name = "Game Thread";
      _thread.IsBackground = true;
    }

    public void Start()
    {
      _gameLoopTimer.Start();
      _packetsTimer.Start();
      
      _socketServer.Start(_serverConfiguration.Port);
      _thread.Start();
    }

    public void Stop()
    {
      _gameLoopTimer.Stop();
      _packetsTimer.Stop();
      _socketServer.Stop();
      _thread.Interrupt();
    }

    private void Execute()
    {
      while (true)
      {
        _dispatcher.Process();
        
        while (_socketServer.ReceiveBuffer.TryDequeue(out var evnt))
        {
          if (evnt.Type == EventType.DISCONNECTED || evnt.Type == EventType.TIMEOUT)
          {
            if (_socketByRooms.Remove(evnt.PeerId, out var room))
              room.Leave(evnt.PeerId);

            _lobby.OnDisconnected(evnt.PeerId);
          }

          if (evnt.Type == EventType.DATA)
          {
            _packets += 1;
            try
            {
              var peerId = evnt.PeerId;
              var data = new ReadOnlySpan<byte>(evnt.Data);
              if (_socketByRooms.TryGetValue(evnt.PeerId, out var room))
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

        var elapsedMilliseconds = _gameLoopTimer.ElapsedMilliseconds;
        if (elapsedMilliseconds > _deltaTime)
        {
          _roomManager.Tick(elapsedMilliseconds / 1000.0f);
          _gameLoopTimer.Restart();
          continue;
        }

        if (_packetsTimer.Elapsed.Seconds > 1)
        {
          _logger.Trace($"Clients: {_socketByRooms.Keys.Count} Packets: {_packets} per sec");
          _packetsTimer.Restart();
          _packets = 0;
        }
        Thread.Sleep(15);
      }
    }

    public void Attach(uint peerId, GameRoom room)
    {
      _socketByRooms.Add(peerId, room);
    }

    public void Detach(uint peerId)
    {
      _socketByRooms.Remove(peerId);
    }

    public void SendSocketEvent(SocketEvent socketEvent)
    {
      _socketServer.SendBuffer.Enqueue(socketEvent);
    }

    public IDispatcher GetDispatcher()
    {
      return _dispatcher;
    }
  }
}