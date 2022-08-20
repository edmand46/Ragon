using System;
using System.Diagnostics;
using System.Timers;
using ENet;
using NLog;

namespace Ragon.Core
{
  public class ENetServer : ISocketServer
  {
    private ILogger _logger = LogManager.GetCurrentClassLogger();
    private Host _host;
    private uint _protocol;
    private Address _address;
    private Event _netEvent;
    private Peer[] _peers;
    private IHandler _handler;
    private Stopwatch _timer;
    
    public ENetServer(IHandler handler)
    {
      _handler = handler;
      _timer = Stopwatch.StartNew();
      _peers = Array.Empty<Peer>();
      _host = new Host();
    }

    public void Start(ushort port, int connections, uint protocol)
    {
      _address = default;
      _address.Port = port;
      _peers = new Peer[connections];
      _protocol = protocol;
      _host.Create(_address, connections, 2, 0, 0, 1024 * 1024);
      
      
      var protocolDecoded = (protocol >> 16 & 0xFF) + "." + (protocol >> 8 & 0xFF) + "." + (protocol & 0xFF);
      _logger.Info($"Network listening on {port}");
      _logger.Info($"Protocol: {protocolDecoded}");
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
        packetFlags = PacketFlags.UnreliableFragmented;
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
          {
            _logger.Trace("None event");
            break;
          }
          case EventType.Connect:
          {
            // if (IsValidProtocol(_netEvent.Data))
            // {
            //   _logger.Warn("Mismatched protocol, close connection");
            //   break;
            // }
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

    private bool IsValidProtocol(uint protocol)
    {
      return protocol == _protocol;
    }
  }
}