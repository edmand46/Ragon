using System;
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

  public class ENetServer : ISocketServer
  {
    public Status Status { get; private set; }

    private ILogger _logger = LogManager.GetCurrentClassLogger();
    private Host _host;
    private Address _address;
    private Event _netEvent;
    private Peer[] _peers;
    private IHandler _handler;

    public ENetServer(IHandler handler)
    {
      _handler = handler;
    }

    public void Start(ushort port, int connections)
    {
      _address = default;
      _address.Port = port;
      _peers = new Peer[connections];

      _host = new Host();
      _host.Create(_address, connections, 2, 0, 0, 1024 * 1024);

      Status = Status.Listening;
      _logger.Info($"Network listening on {port}");
    }

    public void Broadcast(uint[] peersIds, byte[] data, DeliveryType type)
    {
      var newPacket = new Packet();
      var packetFlags = PacketFlags.Instant;
      byte channel = 1;

      if (type == DeliveryType.Reliable)
      {
        packetFlags = PacketFlags.Reliable;
        channel = 0;
      }
      else if (type == DeliveryType.Unreliable)
      {
        channel = 1;
        packetFlags = PacketFlags.None;
      }

      newPacket.Create(data, data.Length, packetFlags);
      foreach (var peerId in peersIds)
        _peers[peerId].Send(channel, ref newPacket);
    }
    
    public void Send(uint peerId, byte[] data, DeliveryType type)
    {
      var newPacket = new Packet();
      var packetFlags = PacketFlags.Instant;
      byte channel = 1;

      if (type == DeliveryType.Reliable)
      {
        packetFlags = PacketFlags.Reliable;
        channel = 0;
      }
      else if (type == DeliveryType.Unreliable)
      {
        channel = 1;
        packetFlags = PacketFlags.None;
      }

      newPacket.Create(data, data.Length, packetFlags);
      _peers[peerId].Send(channel, ref newPacket);
    }

    public void Disconnect(uint peerId, uint errorCode)
    {
      _peers[peerId].Reset();
    }

    public void Process()
    {
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
          case EventType.None:
            Console.WriteLine("None event");
            break;

          case EventType.Connect:
          {
            _peers[_netEvent.Peer.ID] = _netEvent.Peer;
            _handler.OnEvent(_netEvent);
            break;
          }
          case EventType.Disconnect:
          {
            _handler.OnEvent(_netEvent);
            break;
          }
          case EventType.Timeout:
          {
            _handler.OnEvent(_netEvent);
            break;
          }
          case EventType.Receive:
          {
            _handler.OnEvent(_netEvent);
            _netEvent.Packet.Dispose();
            break;
          }
        }
      }
    }

    public void Stop()
    {
      _host?.Dispose();
    }
  }
}