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

using System.Net;
using ENet;
using Ragon.Protocol;
using Ragon.Server.IO;

namespace Ragon.Server.ENetServer;

public sealed class ENetReliableChannel: INetworkChannel
{
  private Peer _peer;
  private byte _channelId;
  private byte[] _data;
  
  public ENetReliableChannel(Peer peer, NetworkChannel channel)
  {
    _peer = peer;
    _data = new byte[1500];
    _channelId = (byte) channel;
  }
  
  public void Send(byte[] data)
  {
    var newPacket = new Packet();
    newPacket.Create(data, data.Length, PacketFlags.Reliable);

    _peer.Send(_channelId, ref newPacket);
  }

  public void Send(RagonBuffer buffer)
  {
    buffer.ToArray(_data);
    
    var newPacket = new Packet();
    newPacket.Create(_data, buffer.Length, PacketFlags.Reliable);
    
    _peer.Send(_channelId, ref newPacket);
  }
}