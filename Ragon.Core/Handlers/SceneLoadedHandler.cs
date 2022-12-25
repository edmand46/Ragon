using NLog;
using Ragon.Common;
using Ragon.Core.Game;
using Ragon.Core.Lobby;

namespace Ragon.Core.Handlers;

public sealed class SceneLoadedHandler : IHandler
{
  private Logger _logger = LogManager.GetCurrentClassLogger();
  
  public void Handle(PlayerContext context, RagonSerializer reader, RagonSerializer writer)
  {
    if (context.LobbyPlayer.Status == LobbyPlayerStatus.Unauthorized)
      return;

    var owner = context.Room.Owner;
    var player = context.RoomPlayer;
    var room = context.Room;

    if (player == owner)
    {
      var statics = reader.ReadUShort();
      for (var staticIndex = 0; staticIndex < statics; staticIndex++)
      {
        var entityType = reader.ReadUShort();
        var eventAuthority = (RagonAuthority) reader.ReadByte();
        var staticId = reader.ReadUShort();
        var propertiesCount = reader.ReadUShort();
      
        var entity = new Entity(player, entityType, staticId, eventAuthority);
        for (var propertyIndex = 0; propertyIndex < propertiesCount; propertyIndex++)
        {
          var propertyType = reader.ReadBool();
          var propertySize = reader.ReadUShort();
          entity.State.AddProperty(new EntityStateProperty(propertySize, propertyType));
        }
        
        _logger.Trace($"Player {context.Connection.Id}|{context.LobbyPlayer.Name} created entity {entity.Id}:{entity.Type}");
        room.AttachEntity(player, entity);
      }

      _logger.Trace($"Player {context.Connection.Id}|{context.LobbyPlayer.Name} loaded");
      
      room.WaitPlayersList.Add(player);

      foreach (var roomPlayer in room.WaitPlayersList)
      {
        DispatchPlayerJoinExcludePlayer(room, roomPlayer, writer);
        
        roomPlayer.SetReady();
      }
      
      room.UpdateReadyPlayerList();
      
      DispatchSnapshot(room, room.WaitPlayersList, writer);
      
      room.WaitPlayersList.Clear();
    }
    else if (owner.IsLoaded)
    {
      player.SetReady();
      
      DispatchPlayerJoinExcludePlayer(room, player, writer);
      
      room.UpdateReadyPlayerList();
      
      DispatchSnapshot(room, new List<RoomPlayer>() { player }, writer);

      foreach (var entity in room.EntityList)
        entity.RestoreBufferedEvents(player, writer);
    }
    else
    {
      _logger.Trace($"Player {player.Connection.Id}|{context.LobbyPlayer.Name} waiting owner of room");
      room.WaitPlayersList.Add(player);
    }
  }

  private void DispatchPlayerJoinExcludePlayer(Room room, RoomPlayer roomPlayer, RagonSerializer writer)
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

  private void DispatchSnapshot(Room room, List<RoomPlayer> receviersList, RagonSerializer writer)
  {
    writer.Clear();
    writer.WriteOperation(RagonOperation.SNAPSHOT);
    writer.WriteUShort((ushort) room.ReadyPlayersList.Count);
    foreach (var roomPlayer in room.ReadyPlayersList)
    {
      writer.WriteUShort(roomPlayer.Connection.Id);
      writer.WriteString(roomPlayer.Id);
      writer.WriteString(roomPlayer.Name);
    }

    var dynamicEntities = room.DynamicEntitiesList;
    var dynamicEntitiesCount = (ushort) dynamicEntities.Count;
    writer.WriteUShort(dynamicEntitiesCount);
    foreach (var entity in dynamicEntities)
      entity.State.Snapshot(writer);

    var staticEntities = room.StaticEntitiesList;
    var staticEntitiesCount = (ushort) staticEntities.Count;
    writer.WriteUShort(staticEntitiesCount);
    foreach (var entity in staticEntities)
      entity.State.Snapshot(writer);

    var sendData = writer.ToArray();
    foreach (var player in receviersList)
      player.Connection.Reliable.Send(sendData);
  }
}