using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NLog;

namespace Ragon.Core
{
  public class Application : IDisposable
  {
    private readonly Logger _logger = LogManager.GetCurrentClassLogger();
    private readonly List<RoomThread> _roomThreads = new();
    private readonly Dictionary<uint, RoomThread> _socketByRoomThreads = new();
    private readonly Dictionary<RoomThread, int> _roomThreadCounter = new();
    private readonly Configuration _configuration;
    private readonly ENetServer _socketServer;
    
    public Application(PluginFactory factory, Configuration configuration, int threadsCount)
    {
      _socketServer = new ENetServer();
      _configuration = configuration;
      
      for (var i = 0; i < threadsCount; i++)
      {
        var roomThread = new RoomThread(factory, configuration);
        _roomThreadCounter.Add(roomThread, 0);
        _roomThreads.Add(roomThread);
      }
    }

    public void Start()
    {
      _socketServer.Start(_configuration.Server.Port);
      
      foreach (var roomThread in _roomThreads)
        roomThread.Start();

      while (true)
      {
        foreach (var roomThread in _roomThreads)
          while (roomThread.ReadOutEvent(out var evnt))
            _socketServer.WriteEvent(evnt);
        
        while (_socketServer.ReadEvent(out var evnt))
        {
          if (evnt.Type == EventType.CONNECTED)
          {
            var roomThread = _roomThreads.First();
            _roomThreadCounter[roomThread] += 1;
            _socketByRoomThreads.Add(evnt.PeerId, roomThread);
          }

          if (_socketByRoomThreads.TryGetValue(evnt.PeerId, out var existsRoomThread))
            existsRoomThread.WriteInEvent(evnt);

          if (evnt.Type == EventType.DISCONNECTED)
          {
            _socketByRoomThreads.Remove(evnt.PeerId, out var roomThread);
            _roomThreadCounter[roomThread] =- 1;
          }

          if (evnt.Type == EventType.TIMEOUT)
          {
            _socketByRoomThreads.Remove(evnt.PeerId, out var roomThread);
            _roomThreadCounter[roomThread] =- 1;
          }
        }

        Thread.Sleep(1);
      }
    }
    
    public void Dispose()
    {
      foreach (var roomThread in _roomThreads)
        roomThread.Dispose();
      
      _roomThreads.Clear();
    }
  }
}