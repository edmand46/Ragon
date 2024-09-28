using System.Collections.Generic;
using System.Linq;
using Ragon.Protocol;
using Ragon.Server;
using Ragon.Server.Entity;
using Ragon.Server.Plugin;
using Ragon.Server.Room;

namespace Ragon.Relay.Entity;

public class RelayRoom: RagonRoom
{
  public Dictionary<ushort, RagonEntity> Entities { get; private set; }
  public List<RagonEntity> DynamicEntitiesList { get; private set; }
  public List<RagonEntity> StaticEntitiesList { get; private set; }
  public List<RagonEntity> EntityList { get; private set; }

  private readonly HashSet<RagonEntity> _entitiesDirtySet;

  public RelayRoom(string roomId, RoomInformation info, IRoomPlugin roomPlugin) : base(roomId, info, roomPlugin)
  {
    Entities = new Dictionary<ushort, RagonEntity>();
    DynamicEntitiesList = new List<RagonEntity>();
    StaticEntitiesList = new List<RagonEntity>();
    EntityList = new List<RagonEntity>();

    _entitiesDirtySet = new HashSet<RagonEntity>();
  }
  
  public void AttachEntity(RagonEntity entity)
  {
    Entities.Add(entity.Id, entity);
    EntityList.Add(entity);

    if (entity.StaticId == 0)
      DynamicEntitiesList.Add(entity);
    else
      StaticEntitiesList.Add(entity);
  }

  public void DetachEntity(RagonEntity entity)
  {
    Entities.Remove(entity.Id);
    EntityList.Remove(entity);
    StaticEntitiesList.Remove(entity);
    DynamicEntitiesList.Remove(entity);

    _entitiesDirtySet.Remove(entity);
  }
  
  
  public void Track(RagonEntity entity)
  {
    _entitiesDirtySet.Add(entity);
  }


  public void OnLeaved(RagonRoomPlayer player)
  {
    // var entitiesToDelete = player.Entities.DynamicList;
    // Writer.WriteUShort((ushort)entitiesToDelete.Count);
    // foreach (var entity in entitiesToDelete)
    // {
    //   Writer.WriteUShort(entity.Id);
    //   DetachEntity(entity);
    // }
    //
    // var sendData = Writer.ToArray();
    // Broadcast(sendData);
  }
  
  public void Tick(float dt)
  {
    var entities = (ushort)_entitiesDirtySet.Count;
    if (entities > 0)
    {
      Writer.Clear();
      Writer.WriteOperation(RagonOperation.REPLICATE_ENTITY_STATE);
      Writer.WriteUShort(entities);

      foreach (var entity in _entitiesDirtySet)
        entity.WriteState(Writer);

      _entitiesDirtySet.Clear();

      var sendData = Writer.ToArray();
      foreach (var roomPlayer in ReadyPlayersList)
        roomPlayer.Connection.Unreliable.Send(sendData);
    }
  }
  
  public IRagonEntity? GetEntityById(ushort id)
  {
    return Entities.TryGetValue(id, out var entity) ? entity : null;
  }

  public IRagonEntity[] GetEntitiesOfPlayer(RagonRoomPlayer player)
  {
    return EntityList.Where(e => e.Owner.Connection.Id == player.Connection.Id).ToArray();
  }

  void Deatach()
  {
    Entities.Clear();
    DynamicEntitiesList.Clear();
    StaticEntitiesList.Clear();
    EntityList.Clear();
    
    _entitiesDirtySet.Clear();
    
    // if (roomPlayer.Connection.Id == Owner.Connection.Id && PlayerList.Count > 0)
    // {
    //   var nextOwner = PlayerList[0];
    //
    //   Owner = nextOwner;
    //
    //   var entitiesToUpdate = roomPlayer.Entities.StaticList;
    //
    //   Writer.Clear();
    //   Writer.WriteOperation(RagonOperation.OWNERSHIP_ENTITY_CHANGED);
    //   Writer.WriteUShort(Owner.Connection.Id);
    //   Writer.WriteUShort((ushort)entitiesToUpdate.Count);
    //
    //   foreach (var entity in entitiesToUpdate)
    //   {
    //     Writer.WriteUShort(entity.Id);
    //
    //     entity.Attach(nextOwner);
    //     nextOwner.Entities.Add(entity);
    //   }
    //
    //   var sendData = Writer.ToArray();
    //   Broadcast(sendData);
    // }
  }
}