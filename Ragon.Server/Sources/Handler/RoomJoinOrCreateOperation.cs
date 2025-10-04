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
using Ragon.Server.Lobby;
using Ragon.Server.Logging;
using Ragon.Server.Plugin;
using Ragon.Server.Room;

namespace Ragon.Server.Handler;

public sealed class RoomJoinOrCreateOperation : BaseOperation
{
  private IRagonLogger _logger = LoggerManager.GetLogger(nameof(RoomJoinOrCreateOperation));

  private readonly RagonRoomParameters _roomParameters = new();
  private readonly IServerPlugin _serverPlugin;
  private readonly RagonServerConfiguration _configuration;
  
  public RoomJoinOrCreateOperation(
    RagonBuffer reader,
    RagonBuffer writer,
    IServerPlugin serverPlugin, 
    RagonServerConfiguration configuration
  ) : base(reader, writer)
  {
    _configuration = configuration;
    _serverPlugin = serverPlugin;
  }

  public override void Handle(RagonContext context, NetworkChannel channel)
  {
    if (context.ConnectionStatus == ConnectionStatus.Unauthorized)
    {
      _logger.Warning("Player not authorized for this request");
      return;
    }

    var roomId = Guid.NewGuid().ToString();
    var lobbyPlayer = context.LobbyPlayer;

    _roomParameters.Deserialize(Reader);

    if (context.Lobby.FindRoomByScene(_roomParameters.Scene, out var existsRoom))
    {
      if (existsRoom.ProjectId != lobbyPlayer.ProjectId)
      {
        _logger.Warning($"Player {context.Connection.Id}|{lobbyPlayer.Name} tried to join room from different project");
        return;
      }

      var player = new RagonRoomPlayer(context, lobbyPlayer.Id, lobbyPlayer.Name);

      context.SetRoom(existsRoom, player);

      JoinSuccess(player, existsRoom, Writer);

      existsRoom.RestoreBufferedEvents(player);

      existsRoom.Plugin.OnPlayerJoined(player);
    }
    else
    {
      var information = new RoomInformation()
      {
        Scene = _roomParameters.Scene,
        Max = _roomParameters.Max,
        Min = _roomParameters.Min,
      };

      if (information.Max > _configuration.LimitPlayersPerRoom)
        information.Max = _configuration.LimitPlayersPerRoom;

      var roomPlayer = new RagonRoomPlayer(context, lobbyPlayer.Id, lobbyPlayer.Name);
      var roomPlugin = _serverPlugin.CreateRoomPlugin(information);
      var room = new RagonRoom(roomId, information, roomPlugin, lobbyPlayer.ProjectId);

      _serverPlugin.OnRoomCreate(lobbyPlayer, room);

      room.Plugin.OnAttached(room);

      context.Lobby.Persist(room);
      context.Scheduler.Run(room);
      context.SetRoom(room, roomPlayer);

      _logger.Trace(
        $"Player {context.Connection.Id}|{context.LobbyPlayer.Name} create room {room.Id} with scene {information.Scene}");

      JoinSuccess(roomPlayer, room, Writer);

      room.Plugin.OnPlayerJoined(roomPlayer);
    }
  }

  private void JoinSuccess(RagonRoomPlayer player, RagonRoom room, RagonBuffer writer)
  {
    writer.Clear();
    writer.WriteOperation(RagonOperation.JOIN_SUCCESS);
    writer.WriteString(room.Id);
    writer.WriteUShort((ushort)room.PlayerMin);
    writer.WriteUShort((ushort)room.PlayerMax);
    writer.WriteString(room.Scene);
    writer.WriteString(player.Id);
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
    player.Connection.Reliable.Send(sendData);

    _logger.Trace($"{player.Connection.Id}|{player.Name} joined to room {room.Id}");
  }
}