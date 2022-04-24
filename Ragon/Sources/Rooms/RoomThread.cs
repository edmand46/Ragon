using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using DisruptorUnity3d;
using ENet;
using NetStack.Serialization;
using Ragon.Common.Protocol;

namespace Ragon.Core
{
  public class RoomThread : IDisposable
  {
    private readonly RoomManager _roomManager;
    private readonly Dictionary<uint, Room> _socketByRooms;
    private readonly Thread _thread;
    private readonly Stopwatch _timer;

    private RingBuffer<Event> _receiveBuffer = new RingBuffer<Event>(8192 + 8192);
    private RingBuffer<Event> _sendBuffer = new RingBuffer<Event>(8192 + 8192);

    public bool ReadOutEvent(out Event evnt) => _sendBuffer.TryDequeue(out evnt);
    public void WriteOutEvent(Event evnt) => _sendBuffer.Enqueue(evnt);

    public bool ReadIntEvent(out Event evnt) => _receiveBuffer.TryDequeue(out evnt);
    public void WriteInEvent(Event evnt) => _receiveBuffer.Enqueue(evnt);

    public RoomThread(PluginFactory factory)
    {
      _thread = new Thread(Execute);
      _thread.IsBackground = true;
      _timer = new Stopwatch();
      _socketByRooms = new Dictionary<uint, Room>();

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
        var deltaTime = _timer.ElapsedMilliseconds;
        if (deltaTime > 1000 / 60)
        {
          while (_receiveBuffer.TryDequeue(out var evnt))
          {
            if (evnt.Type == EventType.DISCONNECTED || evnt.Type == EventType.TIMEOUT)
            {
              
              if (_socketByRooms.ContainsKey(evnt.PeerId))
              {
                _roomManager.Left(evnt.PeerId, Array.Empty<byte>());
                _socketByRooms.Remove(evnt.PeerId);
              }
            }

            if (evnt.Type == EventType.DATA)
            {
              var operation = (RagonOperation) ProtocolHeader.ReadOperation(evnt.Data, 0);
              if (_socketByRooms.TryGetValue(evnt.PeerId, out var room))
              {
                room.ProcessEvent(operation, evnt.PeerId, evnt.Data);
              }
              else
              {
                var payload = new byte[evnt.Data.Length - 2];

                Array.Copy(evnt.Data, 2, payload, 0, evnt.Data.Length - 2);

                _roomManager.ProccessEvent(operation, evnt.PeerId, payload);
              }
            }
          }

          _roomManager.Tick(deltaTime / 1000.0f);

          _timer.Restart();
        }
        else
        {
          Thread.Sleep(1);
        }
      }
    }

    public void Dispose()
    {
    }
  }
}