using Ragon.Common;
using Ragon.Core.Time;

namespace Ragon.Core.Game;

public class Room: IAction
{
  public string Id { get; private set; }
  public RoomInformation Info { get; private set; }
  public RoomPlayer Owner { get; private set; }
  public RagonSerializer Writer { get; }
  public Dictionary<ushort, RoomPlayer> Players { get; private set; }
  public List<RoomPlayer> WaitPlayersList { get; private set; }
  public List<RoomPlayer> ReadyPlayersList { get; private set; }
  public List<RoomPlayer> PlayerList { get; private set; }

  public Dictionary<ushort, Entity> Entities { get; private set; }
  public List<Entity> DynamicEntitiesList { get; private set; }
  public List<Entity> StaticEntitiesList { get; private set; }
  public List<Entity> EntityList { get; private set; }

  private readonly HashSet<Entity> _entitiesDirtySet;

  public Room(string roomId, RoomInformation info)
  {
    Id = roomId;
    Info = info;

    Players = new Dictionary<ushort, RoomPlayer>(info.Max);
    WaitPlayersList = new List<RoomPlayer>(info.Max);
    ReadyPlayersList = new List<RoomPlayer>(info.Max);
    PlayerList = new List<RoomPlayer>(info.Max);

    Entities = new Dictionary<ushort, Entity>();
    DynamicEntitiesList = new List<Entity>();
    StaticEntitiesList = new List<Entity>();
    EntityList = new List<Entity>();

    _entitiesDirtySet = new HashSet<Entity>();
    Writer = new RagonSerializer(512);
  }

  public void AttachEntity(RoomPlayer newOwner, Entity entity)
  {
    Entities.Add(entity.Id, entity);
    EntityList.Add(entity);

    if (entity.StaticId == 0)
      DynamicEntitiesList.Add(entity);
    else
      StaticEntitiesList.Add(entity);

    entity.Create();

    newOwner.Entities.Add(entity);
  }

  public void DetachEntity(RoomPlayer currentOwner, Entity entity, byte[] payload)
  {
    Entities.Remove(entity.Id);
    EntityList.Remove(entity);
    StaticEntitiesList.Remove(entity);
    DynamicEntitiesList.Remove(entity);
    _entitiesDirtySet.Remove(entity);

    entity.Destroy(payload);
    currentOwner.Entities.Remove(entity);
  }

  public void Tick()
  {
    var entities = (ushort) _entitiesDirtySet.Count;
    if (entities > 0)
    {
      Writer.Clear();
      Writer.WriteOperation(RagonOperation.REPLICATE_ENTITY_STATE);
      Writer.WriteUShort(entities);

      foreach (var entity in _entitiesDirtySet)
        entity.State.Write(Writer);

      _entitiesDirtySet.Clear();

      var sendData = Writer.ToArray();
      foreach (var roomPlayer in ReadyPlayersList)
        roomPlayer.Connection.Unreliable.Send(sendData);
    }
  }

  public void AddPlayer(RoomPlayer player)
  {
    if (Players.Count == 0)
      Owner = player;

    player.Attach(this);

    PlayerList.Add(player);
    Players.Add(player.Connection.Id, player);
  }

  public void RemovePlayer(RoomPlayer roomPlayer)
  {
    if (Players.Remove(roomPlayer.Connection.Id, out var player))
    {
      PlayerList.Remove(player);

      {
        Writer.Clear();
        Writer.WriteOperation(RagonOperation.PLAYER_LEAVED);
        Writer.WriteString(player.Id);

        var entitiesToDelete = player.Entities.DynamicList;
        Writer.WriteUShort((ushort) entitiesToDelete.Count);
        foreach (var entity in entitiesToDelete)
        {
          Writer.WriteUShort(entity.Id);
          EntityList.Remove(entity);
        }

        var sendData = Writer.ToArray();
        Broadcast(sendData);
      }
      
      if (roomPlayer.Connection.Id == Owner.Connection.Id && PlayerList.Count > 0)
      {
        var nextOwner = PlayerList[0];
        
        Owner = nextOwner;
        
        var entitiesToUpdate =  roomPlayer.Entities.StaticList;
        
        Writer.Clear();
        Writer.WriteOperation(RagonOperation.OWNERSHIP_CHANGED);
        Writer.WriteString(Owner.Id);
        Writer.WriteUShort((ushort) entitiesToUpdate.Count);
      
        foreach (var entity in entitiesToUpdate)
        {
          Writer.WriteUShort(entity.Id);
        
          entity.SetOwner(nextOwner);
          nextOwner.Entities.Add(entity);
        }

        var sendData = Writer.ToArray();
        Broadcast(sendData);
      }
    }
  }

  public void UpdateReadyPlayerList()
  {
    ReadyPlayersList = PlayerList.Where(p => p.IsLoaded).ToList();
  }

  public void Track(Entity entity)
  {
    _entitiesDirtySet.Add(entity);
  }

  public void Broadcast(byte[] data)
  {
    foreach (var readyPlayer in ReadyPlayersList)
      readyPlayer.Connection.Reliable.Send(data);
  }
}