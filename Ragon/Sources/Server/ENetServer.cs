using System;
using System.Diagnostics;
using System.Threading;
using DisruptorUnity3d;
using ENet;
using NLog;

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

  public class ENetServer
  {
    public Status Status { get; private set; }

    private ILogger _logger = LogManager.GetCurrentClassLogger();
    private Thread _thread;
    private Host _host;
    private Address _address;
    private ENet.Event _netEvent;
    private Peer[] _peers;
    private int _seconds = 0;
    private Stopwatch _packetsTimer;
    public RingBuffer<SocketEvent> SendBuffer;
    public RingBuffer<SocketEvent> ReceiveBuffer;
    
    public void Start(ushort port)
    {
      _address = default;
      _address.Port = port;

      _host = new Host();
      _host.Create(_address, 4095, 2, 0, 0, 1024 * 1024);

      _peers = new Peer[4095];
      
      ReceiveBuffer = new RingBuffer<SocketEvent>(8192 + 8192);
      SendBuffer = new RingBuffer<SocketEvent>(8192 + 8192);

      Status = Status.Listening;
      _packetsTimer = new Stopwatch();
      _thread = new Thread(Execute);
      _thread.Name = "NetworkThread";
      _thread.Start();
      _logger.Info($"Network listening on {port}");
    }

    private void Execute()
    {
      _packetsTimer.Start();
      while (true)
      {
        while (SendBuffer.TryDequeue(out var data))
        {
          if (data.Type == EventType.DATA)
          {
            var newPacket = new Packet();
            var packetFlags = PacketFlags.Instant;
            byte channel = 1;

            if (data.Delivery == DeliveryType.Reliable)
            {
              packetFlags = PacketFlags.Reliable;
              channel = 0;
            }
            else if (data.Delivery == DeliveryType.Unreliable)
            {
              channel = 1;
              packetFlags = PacketFlags.Instant;
            }
            
            newPacket.Create(data.Data, data.Data.Length, packetFlags);
            _peers[data.PeerId].Send(channel, ref newPacket);
          }
          else if (data.Type == EventType.DISCONNECTED)
          {
            _peers[data.PeerId].DisconnectNow(0);
            ReceiveBuffer.Enqueue(data);
          }
        }
      
        bool polled = false;
        while (!polled)
        {
          if (_host.CheckEvents(out _netEvent) <= 0)
          {
            if (_host.Service(15, out _netEvent) <= 0)
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
              var @event = new SocketEvent {PeerId = _netEvent.Peer.ID, Type = EventType.CONNECTED};
              _peers[_netEvent.Peer.ID] = _netEvent.Peer;
              ReceiveBuffer.Enqueue(@event);
              break;
            }
            case ENet.EventType.Disconnect:
            {
              var @event = new SocketEvent {PeerId = _netEvent.Peer.ID, Type = EventType.DISCONNECTED};
              ReceiveBuffer.Enqueue(@event);
              break;
            }
            case ENet.EventType.Timeout:
            {
              var @event = new SocketEvent {PeerId = _netEvent.Peer.ID, Type = EventType.TIMEOUT};
              ReceiveBuffer.Enqueue(@event);
              break;
            }
            case ENet.EventType.Receive:
            {
              var data = new byte[_netEvent.Packet.Length];
              
              _netEvent.Packet.CopyTo(data);
              _netEvent.Packet.Dispose();
              
              var @event = new SocketEvent {PeerId = _netEvent.Peer.ID, Type = EventType.DATA, Data = data };
              ReceiveBuffer.Enqueue(@event);
              break;
            }
          }
        }

        if (_packetsTimer.Elapsed.Seconds > 5)
        {
          Console.WriteLine($"Connections: {_host.PeersCount}");
          _packetsTimer.Restart();
        }
      }
    }

    public void Stop()
    {
      _host?.Dispose();
    }
  }
}