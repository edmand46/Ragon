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

namespace Ragon.Server;

public sealed class RoomJoinOrCreateOperation : IRagonOperation
{
  private RagonRoomParameters _roomParameters = new();
  private Logger _logger = LogManager.GetCurrentClassLogger();
  
  public void Handle(RagonContext context, RagonBuffer reader, RagonBuffer writer)
  {
    if (context.LobbyPlayer.Status == LobbyPlayerStatus.Unauthorized)
    {
      _logger.Warn("Player not authorized for this request");
      return;
    }

    var roomId = Guid.NewGuid().ToString();
    var lobbyPlayer = context.LobbyPlayer;
    
    _roomParameters.Deserialize(reader);

    if (context.Lobby.FindRoomByMap(_roomParameters.Map, out var existsRoom))
    {
      var player = new RagonRoomPlayer(lobbyPlayer.Connection, lobbyPlayer.Id, lobbyPlayer.Name);
      context.SetRoom(existsRoom, player);
      
      JoinSuccess(player, existsRoom, writer);
    }
    else
    {
      var information = new RoomInformation()
      {
        Map = _roomParameters.Map,
        Max = _roomParameters.Max,
        Min = _roomParameters.Min,
      };

      var room = new RagonRoom(roomId, information);
      context.Lobby.Persist(room);
      context.Scheduler.Run(room);
      
      var roomPlayer = new RagonRoomPlayer(lobbyPlayer.Connection, lobbyPlayer.Id, lobbyPlayer.Name);
      context.SetRoom(room, roomPlayer);
      
      _logger.Trace($"Player {context.Connection.Id}|{context.LobbyPlayer.Name} create room {room.Id} {information}");

      JoinSuccess(roomPlayer, room, writer);
    }
  }

  private void JoinSuccess(RagonRoomPlayer player, RagonRoom room, RagonBuffer writer)
  {
    writer.Clear();
    writer.WriteOperation(RagonOperation.JOIN_SUCCESS);
    writer.WriteString(room.Id);
    writer.WriteString(player.Id);
    writer.WriteString(room.Owner.Id);
    writer.WriteUShort((ushort) room.Info.Min);
    writer.WriteUShort((ushort) room.Info.Max);
    writer.WriteString(room.Info.Map);

    var sendData = writer.ToArray();
    player.Connection.Reliable.Send(sendData);
    
    _logger.Trace($"{player.Connection.Id}|{player.Name} joined to room {room.Id}");
  }
}