using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using DisruptorUnity3d;
using NLog;
using Ragon.Common;

namespace Ragon.Core
{
  public class RoomThread : IDisposable
  {
    private readonly RoomManager _roomManager;
    private readonly Dictionary<uint, Room> _socketByRooms;
    private readonly Thread _thread;
    private readonly Stopwatch _timer;
    private readonly ILogger _logger = LogManager.GetCurrentClassLogger();
    private readonly float _deltaTime = 0.0f;
    private readonly RingBuffer<Event> _receiveBuffer = new(2048);
    private readonly RingBuffer<Event> _sendBuffer = new(2048);

    public Configuration Configuration { get; private set; }
    public bool ReadOutEvent(out Event evnt) => _sendBuffer.TryDequeue(out evnt);
    public void WriteOutEvent(Event evnt) => _sendBuffer.Enqueue(evnt);

    public bool ReadIntEvent(out Event evnt) => _receiveBuffer.TryDequeue(out evnt);
    public void WriteInEvent(Event evnt) => _receiveBuffer.Enqueue(evnt);

    public RoomThread(PluginFactory factory, Configuration configuration)
    {
      _thread = new Thread(Execute);
      _thread.IsBackground = true;
      _timer = new Stopwatch();
      _socketByRooms = new Dictionary<uint, Room>();

      Configuration = configuration;
      _deltaTime = 1000.0f / Configuration.Server.TickRate;

      _roomManager = new RoomManager(this, factory);
      _roomManager.OnJoined += (tuple) => _socketByRooms.Add(tuple.Item1, tuple.Item2);
      _roomManager.OnLeaved += (tuple) => _socketByRooms.Remove(tuple.Item1);
    }

    public void Start()
    {
      _timer.Start();
      _thread.Start();
    }

    public void Stop()
    {
      _thread.Interrupt();
    }

    private void Execute()
    {
      while (true)
      {
        while (_receiveBuffer.TryDequeue(out var evnt))
        {
          if (evnt.Type == EventType.DISCONNECTED || evnt.Type == EventType.TIMEOUT)
          {
            if (_socketByRooms.ContainsKey(evnt.PeerId))
            {
              _roomManager.Disconnected(evnt.PeerId);
              _socketByRooms.Remove(evnt.PeerId);
            }
          }

          if (evnt.Type == EventType.DATA)
          {
            var data = new ReadOnlySpan<byte>(evnt.Data);
            var operation = (RagonOperation) data[0];
            var payload = data.Slice(1, data.Length - 1);
            
            if (_socketByRooms.TryGetValue(evnt.PeerId, out var room))
            {
              try
              {
                room.ProcessEvent(operation, evnt.PeerId, payload);
              }
              catch (Exception exception)
              {
                _logger.Error(exception);
              }
            }
            else
            {
              _roomManager.ProcessEvent(operation, evnt.PeerId, payload);
            }
          }
        }

        var elapsedMilliseconds = _timer.ElapsedMilliseconds;
        if (elapsedMilliseconds > _deltaTime)
        {
          _roomManager.Tick(elapsedMilliseconds / 1000.0f);
          _timer.Restart();
          continue;
        }

        Thread.Sleep(15);
      }
    }

    public void Dispose()
    {
    }
  }
}