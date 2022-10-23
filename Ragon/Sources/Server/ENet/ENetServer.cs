﻿using System;
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
    private IEventHandler _eventHandler;
    private Stopwatch _timer;
    
    public ENetServer(IEventHandler eventHandler)
    {
      _eventHandler = eventHandler;
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

    public void Broadcast(ushort[] peersIds, byte[] data, DeliveryType type)
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

    public void Send(ushort peerId, byte[] data, DeliveryType type)
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

    public void Disconnect(ushort peerId, uint errorCode)
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
          if (_host.Service(0, out _netEvent) <= 0)
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
            _eventHandler.OnConnected((ushort)_netEvent.Peer.ID);
            break;
          }
          case EventType.Disconnect:
          {
            _eventHandler.OnDisconnected((ushort)_netEvent.Peer.ID);
            break;
          }
          case EventType.Timeout:
          {
            _eventHandler.OnTimeout((ushort)_netEvent.Peer.ID);
            break;
          }
          case EventType.Receive:
          {
            var peerId = (ushort) _netEvent.Peer.ID;
            var dataRaw = new byte[_netEvent.Packet.Length];
            
            _netEvent.Packet.CopyTo(dataRaw);
            _netEvent.Packet.Dispose();
            
            _eventHandler.OnData(peerId, dataRaw);
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