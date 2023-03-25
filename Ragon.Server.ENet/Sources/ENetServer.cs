﻿/*
 * Copyright 2023 Eduard Kargin <kargin.eduard@gmail.com>
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

using ENet;
using NLog;
using Ragon.Protocol;

namespace Ragon.Server.ENet
{
  public sealed class ENetServer: INetworkServer 
  {
    public Executor Executor => _executor;
    
    private readonly Host _host;
    private readonly ILogger _logger = LogManager.GetCurrentClassLogger();
    
    private ENetConnection[] _connections;
    private INetworkListener _listener;
    private uint _protocol;
    private Event _event;
    private Executor _executor;
    
    public ENetServer()
    {
      _host = new Host();
      _executor = new Executor();
      _connections = Array.Empty<ENetConnection>();
    }

    public void Start(INetworkListener listener, NetworkConfiguration configuration)
    {
      Library.Initialize();
      
      _connections = new ENetConnection[configuration.LimitConnections];
      
      _listener = listener;
      _protocol = configuration.Protocol;
      
      var address = new Address
      {
        Port = (ushort) configuration.Port,
      };

      _host.Create(address, _connections.Length, 2, 0, 0, 1024 * 1024);

      var protocolDecoded = RagonVersion.Parse(_protocol);
      _logger.Info($"Listen at 127.0.0.1:{configuration.Port}");
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
              _logger.Warn($"Mismatched protocol Server: {RagonVersion.Parse(_protocol)} Client: {RagonVersion.Parse(_event.Data)}, close connection");
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
            var peerId = (ushort) _event.Peer.ID;
            var connection = _connections[peerId];
            var dataRaw = new byte[_event.Packet.Length];
            
            _event.Packet.CopyTo(dataRaw);
            _event.Packet.Dispose();
            
            _listener.OnData(connection, dataRaw);
            break;
          }
        }
      }
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
}