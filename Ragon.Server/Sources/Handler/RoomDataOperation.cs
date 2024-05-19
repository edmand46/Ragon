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

using Ragon.Protocol;
using Ragon.Server.IO;

namespace Ragon.Server.Handler;

public sealed class RoomDataOperation : BaseOperation
{
  public RoomDataOperation(RagonBuffer reader, RagonBuffer writer) : base(reader, writer)
  {
  }

  public override void Handle(RagonContext context, NetworkChannel channel)
  {
    var player = context.RoomPlayer;
    var room = context.Room;

    var data = Reader.RawData;
    var dataSize = data.Length - 1;
    var headerSize = 3;
    var size = headerSize + dataSize;
    var sendData = new byte[size];
    var peerId = player.Connection.Id;

    sendData[0] = (byte)RagonOperation.REPLICATE_RAW_DATA;
    sendData[1] = (byte)peerId;
    sendData[2] = (byte)(peerId >> 8);

    var pluginData = new byte[dataSize];
    Array.Copy(data, 1, pluginData, 0, dataSize);
    room.Plugin.OnData(player, pluginData);
    
    Array.Copy(data, 1, sendData, headerSize, dataSize);
    room.Broadcast(sendData, room.ReadyPlayersList, NetworkChannel.RELIABLE);
  }
}