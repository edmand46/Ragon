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

namespace Ragon.Server.ENet;

public sealed class ENetConnection: INetworkConnection
{
  public ushort Id { get; }
  public INetworkChannel Reliable { get; private set; }
  public INetworkChannel Unreliable { get; private set; }
  private Peer _peer;
  
  public ENetConnection(Peer peer)
  {
    _peer = peer;
    
    Id = (ushort) peer.ID;
    Reliable = new ENetReliableChannel(peer, 0);
    Unreliable = new ENetUnreliableChannel(peer, 1);
  }
  
  public void Close()
  {
    _peer.Disconnect(0);
  }
}