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
using Ragon.Server.Entity;
using Ragon.Server.IO;
using Ragon.Server.Lobby;
using Ragon.Server.Room;

namespace Ragon.Server.Handler;

public sealed class SceneLoadedOperation : BaseOperation
{
  private readonly Logger _logger = LogManager.GetCurrentClassLogger();
  
  public SceneLoadedOperation(RagonBuffer reader, RagonBuffer writer): base(reader, writer)
  {
   
  }
  
  public override void Handle(RagonContext context, NetworkChannel channel)
  {
    if (context.ConnectionStatus == ConnectionStatus.Unauthorized)
      return;

    var owner = context.Room.Owner;
    var player = context.RoomPlayer;
    var room = context.Room;
    if (player.IsLoaded)
    {
      _logger.Warn($"Player {player.Name}:{player.Connection.Id} already ready");
      return;
    }

    if (player == owner)
    {
      var statics = Reader.ReadUShort();
      for (var staticIndex = 0; staticIndex < statics; staticIndex++)
      {
        var entityType = Reader.ReadUShort();
        var eventAuthority = (RagonAuthority)Reader.ReadByte();
        var staticId = Reader.ReadUShort();
        var propertiesCount = Reader.ReadUShort();

        var entityParameters = new RagonEntityParameters()
        {
          Type = entityType,
          Authority = eventAuthority,
          AttachId = 0,
          StaticId = staticId,
          BufferedEvents = context.LimitBufferedEvents,
        };

        var entity = new RagonEntity(entityParameters);
        for (var propertyIndex = 0; propertyIndex < propertiesCount; propertyIndex++)
        {
          var propertyType = Reader.ReadBool();
          var propertySize = Reader.ReadUShort();
          entity.AddProperty(new RagonProperty(propertySize, propertyType));
        }
        
        var roomPlugin = room.Plugin;
        if (!roomPlugin.OnEntityCreate(player, entity)) continue;
        
        var playerInfo = $"Player {context.Connection.Id}|{context.LobbyPlayer.Name}";
        var entityInfo = $"{entity.Id}:{entity.Type}";

        _logger.Trace($"{playerInfo} created static entity {entityInfo}");

        entity.Attach(player);
        room.AttachEntity(entity);
        player.AttachEntity(entity);
      }

      _logger.Trace($"Player {context.Connection.Id}|{context.LobbyPlayer.Name} loaded");

      room.WaitPlayersList.Add(player);

      foreach (var roomPlayer in room.WaitPlayersList)
      {
        DispatchPlayerJoinExcludePlayer(room, roomPlayer, Writer);

        roomPlayer.SetReady();
      }

      room.UpdateReadyPlayerList();

      DispatchSnapshot(room, room.WaitPlayersList, Writer);

      room.WaitPlayersList.Clear();
    }
    else if (owner.IsLoaded)
    {
      player.SetReady();

      DispatchPlayerJoinExcludePlayer(room, player, Writer);

      room.UpdateReadyPlayerList();

      DispatchSnapshot(room, new List<RagonRoomPlayer>() { player }, Writer);

      foreach (var entity in room.EntityList)
        entity.RestoreBufferedEvents(player, Writer);
    }
    else
    {
      _logger.Trace($"Player {player.Connection.Id}|{context.LobbyPlayer.Name} waiting owner of room");
      room.WaitPlayersList.Add(player);
    }
  }

  private void DispatchPlayerJoinExcludePlayer(RagonRoom room, RagonRoomPlayer roomPlayer, RagonBuffer writer)
  {
    writer.Clear();
    writer.WriteOperation(RagonOperation.PLAYER_JOINED);
    writer.WriteUShort(roomPlayer.Connection.Id);
    writer.WriteString(roomPlayer.Id);
    writer.WriteString(roomPlayer.Name);

    var sendData = writer.ToArray();
    foreach (var awaiter in room.ReadyPlayersList)
    {
      if (awaiter != roomPlayer)
        awaiter.Connection.Reliable.Send(sendData);
    }
  }

  private void DispatchSnapshot(RagonRoom room, List<RagonRoomPlayer> receviersList, RagonBuffer writer)
  {
    writer.Clear();
    writer.WriteOperation(RagonOperation.SNAPSHOT);
    writer.WriteUShort((ushort)room.ReadyPlayersList.Count);
    foreach (var roomPlayer in room.ReadyPlayersList)
    {
      writer.WriteUShort(roomPlayer.Connection.Id);
      writer.WriteString(roomPlayer.Id);
      writer.WriteString(roomPlayer.Name);
    }

    var dynamicEntities = room.DynamicEntitiesList;
    var dynamicEntitiesCount = (ushort)dynamicEntities.Count;
    writer.WriteUShort(dynamicEntitiesCount);
    foreach (var entity in dynamicEntities)
      entity.Snapshot(writer);

    var staticEntities = room.StaticEntitiesList;
    var staticEntitiesCount = (ushort)staticEntities.Count;
    writer.WriteUShort(staticEntitiesCount);
    foreach (var entity in staticEntities)
      entity.Snapshot(writer);

    var sendData = writer.ToArray();
    foreach (var player in receviersList)
      player.Connection.Reliable.Send(sendData);
  }
}