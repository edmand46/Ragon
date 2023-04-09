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
using Ragon.Server.Web;

namespace Ragon.Server.Handler;

public sealed class RoomJoinOperation : IRagonOperation
{
  private readonly Logger _logger = LogManager.GetCurrentClassLogger();
  private readonly IServerPlugin _serverPlugin;
  private readonly WebHookPlugin _webHookPlugin;
  
  public RoomJoinOperation(IServerPlugin serverPlugin, WebHookPlugin plugin)
  {
    _serverPlugin = serverPlugin;
    _webHookPlugin = plugin;
  }

  public void Handle(RagonContext context, RagonBuffer reader, RagonBuffer writer)
  {
    var roomId = reader.ReadString();
    var lobbyPlayer = context.LobbyPlayer;

    if (!context.Lobby.FindRoomById(roomId, out var existsRoom))
    {
      JoinFailed(context, writer);
      
      _logger.Trace($"Player {context.Connection.Id}|{context.LobbyPlayer.Name} failed to join room {roomId}");
      return;
    }

    var player = new RagonRoomPlayer(context.Connection, lobbyPlayer.Id, lobbyPlayer.Name);
    context.SetRoom(existsRoom, player);
    
    if (!_serverPlugin.OnRoomJoin(player, existsRoom))
      return;
    
    _webHookPlugin.RoomJoined(context, existsRoom, player);
    
    JoinSuccess(context, existsRoom, writer);
    
    _logger.Trace($"Player {context.Connection.Id}|{context.LobbyPlayer.Name} joined to {existsRoom.Id}");
  }

  private void JoinSuccess(RagonContext context, RagonRoom room, RagonBuffer writer)
  {
    writer.Clear();
    writer.WriteOperation(RagonOperation.JOIN_SUCCESS);
    writer.WriteString(room.Id);
    writer.WriteString(context.RoomPlayer.Id);
    writer.WriteString(room.Owner.Id);
    writer.WriteUShort((ushort) room.PlayerMin);
    writer.WriteUShort((ushort) room.PlayerMax);
    writer.WriteString(room.Map);

    var sendData = writer.ToArray();
    context.Connection.Reliable.Send(sendData);
  }

  private void JoinFailed(RagonContext context, RagonBuffer writer)
  {
    writer.Clear();
    writer.WriteOperation(RagonOperation.JOIN_FAILED);
    writer.WriteString($"Room not exists");

    var sendData = writer.ToArray();
    context.Connection.Reliable.Send(sendData);
  }
}