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

using NLog;
using Ragon.Protocol;

namespace Ragon.Server.Handler;

public sealed class RoomDataOperation : BaseOperation
{
  public RoomDataOperation(RagonBuffer reader, RagonBuffer writer) : base(reader, writer)
  {
  }

  public override void Handle(RagonContext context)
  {
    var player = context.RoomPlayer;
    var room = context.Room;
    
    var data = Reader.RawData;
    
    Writer.Clear();
    Writer.WriteOperation(RagonOperation.REPLICATE_RAW_DATA);
    Writer.WriteUShort(player.Connection.Id);
    
    var playerData = Writer.ToArray();
    var payloadData = data;
    var size = playerData.Length + payloadData.Length;
    var sendData = new byte[size];
    
    Array.Copy(playerData, 0, sendData, 0, playerData.Length);
    Array.Copy(payloadData, 0, sendData, playerData.Length, payloadData.Length);
    
    room.Broadcast(sendData);
  }
}