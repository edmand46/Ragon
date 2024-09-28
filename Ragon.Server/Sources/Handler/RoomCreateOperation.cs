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

namespace Ragon.Server.Handler
{
  public sealed class RoomCreateOperation : BaseOperation
  {
    private IRagonLogger _logger = LoggerManager.GetLogger(nameof(RoomCreateOperation));

    private readonly RagonRoomParameters _roomParameters = new();
    private readonly IServerPlugin _serverPlugin;
    private readonly RagonServerConfiguration _configuration;

    public RoomCreateOperation(
      RagonStream reader,
      RagonStream writer,
      IServerPlugin serverPlugin,
      RagonServerConfiguration configuration 
    ) : base(reader,
      writer)
    {
      _configuration = configuration;
      _serverPlugin = serverPlugin;
    }

    public override void Handle(RagonContext context, NetworkChannel channel)
    {
      if (context.ConnectionStatus == ConnectionStatus.Unauthorized)
      {
        _logger.Warning($"Player {context.Connection.Id} not authorized for this request");
        return;
      }

      var custom = Reader.ReadBool();
      var roomId = Guid.NewGuid().ToString();

      if (custom)
      {
        roomId = Reader.ReadString();
        if (context.Lobby.FindRoomById(roomId, out _))
        {
          Writer.Clear();
          Writer.WriteOperation(RagonOperation.JOIN_FAILED);
          Writer.WriteString($"Room with id {roomId} already exists");

          var sendData = Writer.ToArray();
          context.Connection.Reliable.Send(sendData);

          _logger.Trace(
            $"Player {context.Connection.Id}|{context.LobbyPlayer.Name} join failed to room {roomId}, room already exist");
          return;
        }
      }

      _roomParameters.Deserialize(Reader);

      var information = new RoomInformation()
      {
        Scene = _roomParameters.Scene,
        Max = _roomParameters.Max,
        Min = _roomParameters.Min,
      };

      if (information.Max > _configuration.LimitPlayersPerRoom)
        information.Max = _configuration.LimitPlayersPerRoom;
      
      var lobbyPlayer = context.LobbyPlayer;
      var roomPlayer = new RagonRoomPlayer(context, lobbyPlayer.Id, lobbyPlayer.Name);

      var roomPlugin = _serverPlugin.CreateRoomPlugin(information);
      var room = new RagonRoom(roomId, information, roomPlugin);

      room.Plugin.OnAttached(room);
      roomPlayer.OnAttached(room);

      context.Scheduler.Run(room);
      context.Lobby.Persist(room);
      context.SetRoom(room, roomPlayer);

      _logger.Trace(
        $"Player {context.Connection.Id}|{context.LobbyPlayer.Name} create room {room.Id} with scene {information.Scene}");

      JoinSuccess(roomPlayer, room, Writer);

      roomPlugin.OnPlayerJoined(roomPlayer);

      _logger.Trace($"Player {context.Connection.Id}|{context.LobbyPlayer.Name} joined to room {room.Id}");
    }

    private void JoinSuccess(RagonRoomPlayer player, RagonRoom room, RagonStream writer)
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
    }
  }
}