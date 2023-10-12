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
using Ragon.Server.IO;
using Ragon.Server.Lobby;
using Ragon.Server.Plugin;
using Ragon.Server.Plugin.Web;
using Ragon.Server.Room;

namespace Ragon.Server.Handler;

public sealed class RoomJoinOrCreateOperation : BaseOperation
{
  private readonly RagonRoomParameters _roomParameters = new();
  private readonly Logger _logger = LogManager.GetCurrentClassLogger();
  private readonly IServerPlugin _serverPlugin;
  private readonly RagonWebHookPlugin _ragonWebHookPlugin;
  
  public RoomJoinOrCreateOperation(RagonBuffer reader, RagonBuffer writer, IServerPlugin serverPlugin, RagonWebHookPlugin plugin): base(reader, writer)
  {
    _serverPlugin = serverPlugin;
    _ragonWebHookPlugin = plugin;
  }
  
  public override void Handle(RagonContext context, NetworkChannel channel)
  {
    if (context.ConnectionStatus == ConnectionStatus.Unauthorized)
    {
      _logger.Warn("Player not authorized for this request");
      return;
    }

    var roomId = Guid.NewGuid().ToString();
    var lobbyPlayer = context.LobbyPlayer;
    
    _roomParameters.Deserialize(Reader);

    if (context.Lobby.FindRoomByScene(_roomParameters.Scene, out var existsRoom))
    {
      var player = new RagonRoomPlayer(context.Connection, lobbyPlayer.Id, lobbyPlayer.Name);
      context.SetRoom(existsRoom, player);
      
      _ragonWebHookPlugin.RoomJoined(context, existsRoom, player);
      
      JoinSuccess(player, existsRoom, Writer);
    }
    else
    {
      var information = new RoomInformation()
      {
        Scene = _roomParameters.Scene,
        Max = _roomParameters.Max,
        Min = _roomParameters.Min,
      };

      var roomPlayer = new RagonRoomPlayer(context.Connection, lobbyPlayer.Id, lobbyPlayer.Name);
      var roomPlugin = _serverPlugin.CreateRoomPlugin(information);
      var room = new RagonRoom(roomId, information, roomPlugin);
      
      _ragonWebHookPlugin.RoomCreated(context, room, roomPlayer);
      
      context.Lobby.Persist(room);
      context.Scheduler.Run(room);
      context.SetRoom(room, roomPlayer);
      
      _logger.Trace($"Player {context.Connection.Id}|{context.LobbyPlayer.Name} create room {room.Id} with scene {information.Scene}");

      JoinSuccess(roomPlayer, room, Writer);
    }
  }

  private void JoinSuccess(RagonRoomPlayer player, RagonRoom room, RagonBuffer writer)
  {
    writer.Clear();
    writer.WriteOperation(RagonOperation.JOIN_SUCCESS);
    writer.WriteString(room.Id);
    writer.WriteString(player.Id);
    writer.WriteString(room.Owner.Id);
    writer.WriteUShort((ushort) room.PlayerMin);
    writer.WriteUShort((ushort) room.PlayerMax);
    writer.WriteString(room.Scene);

    var sendData = writer.ToArray();
    player.Connection.Reliable.Send(sendData);
    
    _logger.Trace($"{player.Connection.Id}|{player.Name} joined to room {room.Id}");
  }
}