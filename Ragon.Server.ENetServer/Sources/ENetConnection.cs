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

using ENet;
using Ragon.Server.IO;

namespace Ragon.Server.ENetServer;

public sealed class ENetConnection: INetworkConnection
{
  private static ushort _iterator = 0;
  public ushort Id { get; }
  public INetworkChannel Reliable { get; private set; }
  public INetworkChannel Unreliable { get; private set; }
  private Peer _peer;
  
  public ENetConnection(Peer peer)
  {
    _peer = peer;
    
    // Id = (ushort) peer.ID;
    Id = _iterator++;
    Reliable = new ENetReliableChannel(peer, NetworkChannel.RELIABLE);
    Unreliable = new ENetUnreliableChannel(peer, NetworkChannel.UNRELIABLE);
  }
  
  public void Close()
  {
    _peer.Disconnect(0);
  }
}