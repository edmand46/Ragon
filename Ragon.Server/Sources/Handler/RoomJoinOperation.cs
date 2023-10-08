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
using Ragon.Server.Plugin;
using Ragon.Server.Plugin.Web;
using Ragon.Server.Room;

namespace Ragon.Server.Handler;

public sealed class RoomJoinOperation : BaseOperation
{
  private readonly Logger _logger = LogManager.GetCurrentClassLogger();
  private readonly RagonWebHookPlugin _webHook;

  public RoomJoinOperation(RagonBuffer reader, RagonBuffer writer, RagonWebHookPlugin plugin) : base(reader, writer)
  {
    _webHook = plugin;
  }

  
  public override void Handle(RagonContext context)
  {
    var roomId = Reader.ReadString();
    var lobbyPlayer = context.LobbyPlayer;

    if (!context.Lobby.FindRoomById(roomId, out var existsRoom))
    {
      JoinFailed(context, Writer);

      _logger.Trace($"Player {context.Connection.Id}|{context.LobbyPlayer.Name} failed to join room {roomId}");
      return;
    }

    var player = new RagonRoomPlayer(context.Connection, lobbyPlayer.Id, lobbyPlayer.Name);
    context.SetRoom(existsRoom, player);

    if (!existsRoom.Plugin.OnPlayerJoined(player))
      return;

    _webHook.RoomJoined(context, existsRoom, player);

    JoinSuccess(context, existsRoom, Writer);

    _logger.Trace($"Player {context.Connection.Id}|{context.LobbyPlayer.Name} joined to {existsRoom.Id}");
  }

  private void JoinSuccess(RagonContext context, RagonRoom room, RagonBuffer writer)
  {
    writer.Clear();
    writer.WriteOperation(RagonOperation.JOIN_SUCCESS);
    writer.WriteString(room.Id);
    writer.WriteString(context.RoomPlayer.Id);
    writer.WriteString(room.Owner.Id);
    writer.WriteUShort((ushort)room.PlayerMin);
    writer.WriteUShort((ushort)room.PlayerMax);
    writer.WriteString(room.Scene);

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