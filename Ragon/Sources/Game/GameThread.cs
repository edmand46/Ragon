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
    private readonly Dictionary<uint, GameRoom> _socketByRooms;
    private readonly RoomManager _roomManager;
    private readonly ISocketServer _server;
    private readonly Thread _thread;
    private readonly Server _serverConfiguration;
    private readonly Stopwatch _gameLoopTimer;
    private readonly Lobby _lobby;
    private readonly ILogger _logger = LogManager.GetCurrentClassLogger();
    private readonly float _deltaTime = 0.0f;
    private readonly Stopwatch _packetsTimer;
    private int _packets = 0;
    
    public IDispatcher Dispatcher { get; private set; }
    public ISocketServer Server { get; private set; }
    
    public GameThread(PluginFactory factory, Configuration configuration)
    {
      var authorizationProvider = factory.CreateAuthorizationProvider(configuration);
      
      Dispatcher = new Dispatcher();
      Server = new ENetServer(this);
      
      _serverConfiguration = configuration.Server;
      _deltaTime = 1000.0f / configuration.TickRate;
      
      _roomManager = new RoomManager(factory, this);
      _lobby = new Lobby(authorizationProvider, _roomManager, this);
      
      _gameLoopTimer = new Stopwatch();
      _packetsTimer = new Stopwatch();
      _socketByRooms = new Dictionary<uint, GameRoom>();

      _thread = new Thread(Execute);
      _thread.Name = "Game Thread";
      _thread.IsBackground = true;
    }

    public void Start()
    {
      Server.Start(_serverConfiguration.Port);
      
      _gameLoopTimer.Start();
      _packetsTimer.Start();
      _thread.Start();
    }

    public void Stop()
    {
      Server.Stop();
      
      _gameLoopTimer.Stop();
      _packetsTimer.Stop();
      _thread.Interrupt();
    }

    private void Execute()
    {
      while (true)
      {
        Server.Process();
        Dispatcher.Process();
        
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

    public void OnEvent(Event evnt)
    {
      if (evnt.Type == ENet.EventType.Timeout || evnt.Type == ENet.EventType.Disconnect)
      {
        if (_socketByRooms.Remove(evnt.Peer.ID, out var room))
          room.Leave(evnt.Peer.ID);

        _lobby.OnDisconnected(evnt.Peer.ID);
      }

      if (evnt.Type == ENet.EventType.Receive)
      {
        _packets += 1;
        try
        {
          var peerId = evnt.Peer.ID;
          var dataRaw = new byte[evnt.Packet.Length];
          evnt.Packet.CopyTo(dataRaw);
          
          var data = new ReadOnlySpan<byte>(dataRaw);
          if (_socketByRooms.TryGetValue(peerId, out var room))
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