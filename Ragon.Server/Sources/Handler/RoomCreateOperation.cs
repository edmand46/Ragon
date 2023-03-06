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

public sealed class RoomCreateOperation: IRagonOperation
{
  private RagonRoomParameters _roomParameters = new();
  private Logger _logger = LogManager.GetCurrentClassLogger();
  
  public void Handle(RagonContext context, RagonBuffer reader, RagonBuffer writer)
  {
    if (context.LobbyPlayer.Status == LobbyPlayerStatus.Unauthorized)
    {
      _logger.Warn($"Player {context.Connection.Id} not authorized for this request");
      return;
    }

    var custom = reader.ReadBool();
    var roomId = Guid.NewGuid().ToString();
    
    if (custom)
    {
      roomId = reader.ReadString();
      if (context.Lobby.FindRoomById(roomId, out _))
      { 
        writer.Clear();
        writer.WriteOperation(RagonOperation.JOIN_FAILED);
        writer.WriteString($"Room with id {roomId} already exists");
            
        var sendData = writer.ToArray();
        context.Connection.Reliable.Send(sendData);
        
        _logger.Trace($"Player {context.Connection.Id}|{context.LobbyPlayer.Name} join failed to room {roomId}, room already exist");
        return;        
      }
    }
    
    _roomParameters.Deserialize(reader);
    
    var information = new RoomInformation()
    {
        Map = _roomParameters.Map,
        Max = _roomParameters.Max,
        Min = _roomParameters.Min,
    };

    var lobbyPlayer = context.LobbyPlayer;
    
    var room = new RagonRoom(roomId, information);
    context.Scheduler.Run(room);
    context.Lobby.Persist(room);
    
    var player = new RagonRoomPlayer(lobbyPlayer.Connection, lobbyPlayer.Id, lobbyPlayer.Name);
    context.SetRoom(room, player);
    
    _logger.Trace($"Player {context.Connection.Id}|{context.LobbyPlayer.Name} create room {room.Id} {information}");
    
    JoinSuccess(player, room, writer);
    
    _logger.Trace($"Player {context.Connection.Id}|{context.LobbyPlayer.Name} joined to room {room.Id}");
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
  }
}