/*
 * Copyright 2023-2024 Eduard Kargin <kargin.eduard@gmail.com>
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using ENet;
using Ragon.Protocol;
using Ragon.Server.IO;
using Ragon.Server.Logging;

namespace Ragon.Transport;

public sealed class ENetServer : INetworkServer
{
  private readonly Host _host = new();
  private readonly IRagonLogger _logger = LoggerManager.GetLogger(nameof(ENetServer));

  private ENetConnection[] _connections = Array.Empty<ENetConnection>();
  private INetworkListener _listener;
  private uint _protocol;
  private Event _event;

  public void Listen(INetworkListener listener, NetworkConfiguration configuration)
  {
    Library.Initialize();

    _connections = new ENetConnection[configuration.LimitConnections];

    _listener = listener;
    _protocol = configuration.Protocol;

    var address = new Address
    {
      Port = (ushort)configuration.Port,
    };

    _host.Create(address, _connections.Length, 2, 0, 0, 1024 * 1024);

    var protocolDecoded = RagonVersion.Parse(_protocol);
    _logger.Info($"Listen at {configuration.Address}:{configuration.Port}");
    _logger.Info($"Protocol: {protocolDecoded}");
  }

  public void Update()
  {
    bool polled = false;
    while (!polled)
    {
      if (_host.CheckEvents(out _event) <= 0)
      {
        if (_host.Service(0, out _event) <= 0)
          break;

        polled = true;
      }

      switch (_event.Type)
      {
        case EventType.None:
        {
          _logger.Trace("None event");
          break;
        }
        case EventType.Connect:
        {
          if (!IsValidProtocol(_event.Data))
          {
            _logger.Warning(
              $"Mismatched protocol Server: {RagonVersion.Parse(_protocol)} Client: {RagonVersion.Parse(_event.Data)}, close connection");
            _event.Peer.DisconnectNow(0);
            break;
          }

          var connection = new ENetConnection(_event.Peer);

          _connections[_event.Peer.ID] = connection;
          _listener.OnConnected(connection);
          break;
        }
        case EventType.Disconnect:
        {
          var connection = _connections[_event.Peer.ID];
          _listener.OnDisconnected(connection);
          break;
        }
        case EventType.Timeout:
        {
          var connection = _connections[_event.Peer.ID];
          _listener.OnTimeout(connection);
          break;
        }
        case EventType.Receive:
        {
          var peerId = (ushort)_event.Peer.ID;
          var connection = _connections[peerId];
          var dataRaw = new byte[_event.Packet.Length];

          _event.Packet.CopyTo(dataRaw);
          _event.Packet.Dispose();

          _listener.OnData(connection, (NetworkChannel)_event.ChannelID, dataRaw);
          break;
        }
      }
    }
  }

  public void Broadcast(byte[] data, NetworkChannel channel)
  {
    var packet = new Packet();
    var flag = channel == NetworkChannel.RELIABLE ? PacketFlags.Reliable : PacketFlags.None;

    packet.Create(data, flag);

    _host.Broadcast((byte)channel, ref packet);
  }

  public void Stop()
  {
    _host?.Dispose();

    Library.Deinitialize();
  }

  private bool IsValidProtocol(uint protocol)
  {
    return protocol == _protocol;
  }
}