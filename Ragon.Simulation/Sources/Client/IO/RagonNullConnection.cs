/*
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
using Ragon.Protocol;
using Event = ENet.Event;
using EventType = ENet.EventType;

namespace Ragon.Client
{
  public class RagonNullConnection : INetworkConnection
  {
    public ushort Id { get; }
    
    public NetworkStatistics Statistics { get; private set; }
    public INetworkChannel Reliable { get; private set; }
    public INetworkChannel Unreliable { get; private set; }
    
    public Action<byte[]> OnData { get; set; }
    public Action OnConnected { get; set; }
    public Action<RagonDisconnect> OnDisconnected { get; set; }
    public ulong BytesSent { get; }
    public ulong BytesReceived { get; }
    public int Ping { get; }

    private static bool _libraryLoaded = false;
    private Host _host;
    private Peer _peer;
    private Event _netEvent;
    
    public RagonNullConnection()
    {
      _host = new Host();
      _host.Create();
    }


    public void Prepare()
    {
      if (!_libraryLoaded)
      {
        Library.Initialize();
        _libraryLoaded = true;
      }
    }

    public void Disconnect()
    {
      if (_peer.IsSet)
        _peer.DisconnectNow(0);
    }

    public void Connect(string server, ushort port, uint protocol)
    {
      Address address = new Address();
      address.SetHost(server);
      address.Port = port;

      _peer = _host.Connect(address, 2, protocol);
      _peer.Timeout(32, 5000, 5000);
      
      Statistics = new NetworkStatistics();
      Reliable = new NullReliableChannel(_netEvent.Peer, 0);
      Unreliable = new NullUnreliableChannel(_netEvent.Peer, 1);
    }

    public void Update()
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
            break;
          case EventType.Connect:

            OnConnected?.Invoke();
            break;
          case EventType.Disconnect:
            OnDisconnected?.Invoke(RagonDisconnect.SERVER);
            break;
          case EventType.Timeout:
            OnDisconnected?.Invoke(RagonDisconnect.TIMEOUT);
            break;
          case EventType.Receive:
            var data = new byte[_netEvent.Packet.Length];

            _netEvent.Packet.CopyTo(data);
            _netEvent.Packet.Dispose();

            OnData?.Invoke(data);
            break;
        }
      }
    }

    public void Dispose()
    {
      if (_host.IsSet)
      {
        _host?.Flush();
        _host?.Dispose();
      }

      if (_libraryLoaded)
        Library.Deinitialize();
    }

    public void Close()
    {
      
    }
  }
}