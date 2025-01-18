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
using Ragon.Server.Logging;
using Ragon.Server.Room;

namespace Ragon.Server.Handler;

public sealed class RoomJoinOperation : BaseOperation
{
  private readonly IRagonLogger _logger = LoggerManager.GetLogger(nameof(RoomJoinOperation));

  public RoomJoinOperation(RagonStream reader, RagonStream writer) : base(reader, writer)
  {
  }
  
  public override void Handle(RagonContext context, NetworkChannel channel)
  {
    var roomId = Reader.ReadString();
    var lobbyPlayer = context.LobbyPlayer;

    if (!context.Lobby.FindRoomById(roomId, out var existsRoom))
    {
      JoinFailed(context, Writer);

      _logger.Trace($"Player {context.Connection.Id}|{context.LobbyPlayer.Name} failed to join room {roomId}");
      return;
    }

    var player = new RagonRoomPlayer(context, lobbyPlayer.Id, lobbyPlayer.Name);
    context.SetRoom(existsRoom, player);
    
    JoinSuccess(context, existsRoom, Writer);

    existsRoom.RestoreBufferedEvents(player);
    existsRoom.Plugin.OnPlayerJoined(player);
    
    _logger.Trace($"Player {context.Connection.Id}|{context.LobbyPlayer.Name} joined to {existsRoom.Id}");
  }

  private void JoinSuccess(RagonContext context, RagonRoom room, RagonStream writer)
  {
    writer.Clear();
    writer.WriteOperation(RagonOperation.JOIN_SUCCESS);
    writer.WriteString(room.Id);
    writer.WriteUShort((ushort)room.PlayerMin);
    writer.WriteUShort((ushort)room.PlayerMax);
    writer.WriteString(context.RoomPlayer.Id);
    writer.WriteString(room.Owner.Id);
    
    room.UserData.Snapshot(writer);
      
    writer.WriteUShort((ushort)room.PlayerList.Count);
    foreach (var roomPlayer in room.PlayerList)
    {
      writer.WriteUShort(roomPlayer.Connection.Id);
      writer.WriteString(roomPlayer.Id);
      writer.WriteString(roomPlayer.Name);
      
      roomPlayer.Context.UserData.Snapshot(writer);
    }
    
    var sendData = writer.ToArray();
    context.Connection.Reliable.Send(sendData);
  }

  private void JoinFailed(RagonContext context, RagonStream writer)
  {
    writer.Clear();
    writer.WriteOperation(RagonOperation.JOIN_FAILED);
    writer.WriteString($"Room not exists");

    var sendData = writer.ToArray();
    context.Connection.Reliable.Send(sendData);
  }
}