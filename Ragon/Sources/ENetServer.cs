using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using DisruptorUnity3d;
using ENet;
using NLog;
using NLog.LayoutRenderers.Wrappers;

namespace Ragon.Core
{
  public enum Status
  {
    Stopped,
    Listening,
    Disconnecting,
    Connecting,
    Assigning,
    Connected
  }

  public enum DeliveryType
  {
    UnreliableUnsequenced,
    UnreliableSequenced,
    UnreliableFragmented,
    ReliableSequenced
  }


  public class ENetServer : IDisposable
  {
    public Status Status { get; private set; }

    private ILogger _logger = LogManager.GetCurrentClassLogger();
    private Thread _thread;
    private Host _host;
    private Address _address;
    private ENet.Event _netEvent;
    private Peer[] _peers;
    private RingBuffer<Event> _receiveBuffer;
    private RingBuffer<Event> _sendBuffer;
    public void WriteEvent(Event evnt) => _sendBuffer.Enqueue(evnt);
    public bool ReadEvent(out Event evnt) => _receiveBuffer.TryDequeue(out evnt);
    
    public void Start(ushort port)
    {
      Library.Initialize();

      _address = default;
      _address.Port = port;

      _host = new Host();
      _host.Create(_address, 4095, 2, 0, 0, 1024 * 1024);

      _peers = new Peer[4095];
      _sendBuffer = new RingBuffer<Event>(8192 + 8192);
      _receiveBuffer = new RingBuffer<Event>(8192 + 8192);

      Status = Status.Listening;

      _thread = new Thread(Execute);
      _thread.Name = "NetworkThread";
      _thread.Start();
      _logger.Info($"Socket Server Started at port {port}");
    }

    private void Execute()
    {
      while (true)
      {
        while (_sendBuffer.TryDequeue(out var data))
        {
          var newPacket = new Packet();
          newPacket.Create(data.Data, data.Data.Length, PacketFlags.Reliable);
          _peers[data.PeerId].Send(0, ref newPacket);
        }
        
        bool polled = false;
        while (!polled)
        {
          if (_host.CheckEvents(out _netEvent) <= 0)
          {
            if (_host.Service(16, out _netEvent) <= 0)
              break;

            polled = true;
          }

          switch (_netEvent.Type)
          {
            case ENet.EventType.None:
              Console.WriteLine("None event");
              break;

            case ENet.EventType.Connect:
            {
              var @event = new Event {PeerId = _netEvent.Peer.ID, Type = EventType.CONNECTED};
              // Console.WriteLine("Client connected - ID: " + _netEvent.Peer.ID + ", IP: " + _netEvent.Peer.IP);
              _peers[_netEvent.Peer.ID] = _netEvent.Peer;
              _receiveBuffer.Enqueue(@event);
              break;
            }
            case ENet.EventType.Disconnect:
            {
              var @event = new Event {PeerId = _netEvent.Peer.ID, Type = EventType.DISCONNECTED};
              // Console.WriteLine("Client disconnected - ID: " + _netEvent.Peer.ID + ", IP: " + _netEvent.Peer.IP);
              _receiveBuffer.Enqueue(@event);
              break;
            }
            case ENet.EventType.Timeout:
            {
              // Console.WriteLine("Client timeout - ID: " + _netEvent.Peer.ID + ", IP: " + _netEvent.Peer.IP);
              var @event = new Event {PeerId = _netEvent.Peer.ID, Type = EventType.TIMEOUT};
              // Console.WriteLine("Client disconnected - ID: " + _netEvent.Peer.ID + ", IP: " + _netEvent.Peer.IP);
              _receiveBuffer.Enqueue(@event);
              break;
            }
            case ENet.EventType.Receive:
            {
              // Console.WriteLine("Packet received from - ID: " + _netEvent.Peer.ID + ", IP: " + _netEvent.Peer.IP + ", Channel ID: " + _netEvent.ChannelID + ", Data length: " + _netEvent.Packet.Length);
              var data = new byte[_netEvent.Packet.Length];
              _netEvent.Packet.CopyTo(data);
              _netEvent.Packet.Dispose();

              var @event = new Event {PeerId = _netEvent.Peer.ID, Type = EventType.DATA, Data = data};
              _receiveBuffer.Enqueue(@event);
              break;
            }
          }
        }
      }
    }

    public void Dispose()
    {
      Library.Deinitialize();

      _host?.Dispose();
    }
  }
}