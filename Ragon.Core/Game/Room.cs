using Ragon.Common;

namespace Ragon.Core.Game;

public class Room
{
  public string Id { get; }
  public RoomInformation Info { get; }
  public RoomPlayer Owner { get; set; }

  public Dictionary<ushort, RoomPlayer> Players { get; }
  public List<RoomPlayer> WaitPlayersList { get; private set; }
  public List<RoomPlayer> ReadyPlayersList { get; private set; }
  public List<RoomPlayer> PlayerList { get; private set; }

  public Dictionary<ushort, Entity> Entities { get; private set; }
  public List<Entity> DynamicEntitiesList { get; private set; }
  public List<Entity> StaticEntitiesList { get; private set; }
  public List<Entity> EntityList { get; private set; }

  private HashSet<Entity> _entitiesDirtySet;
  private RagonSerializer _writer;

  public RagonSerializer Writer => _writer;
  public PluginBase Plugin { get; set; }

  public Room(string roomId, RoomInformation info, PluginBase plugin)
  {
    Id = roomId;
    Info = info;

    Plugin = plugin;

    Players = new Dictionary<ushort, RoomPlayer>(info.Max);
    WaitPlayersList = new List<RoomPlayer>(info.Max);
    ReadyPlayersList = new List<RoomPlayer>(info.Max);
    PlayerList = new List<RoomPlayer>(info.Max);

    Entities = new Dictionary<ushort, Entity>();
    DynamicEntitiesList = new List<Entity>();
    StaticEntitiesList = new List<Entity>();
    EntityList = new List<Entity>();

    _entitiesDirtySet = new HashSet<Entity>();
    _writer = new RagonSerializer(512);
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

    entity.Destroy();
    currentOwner.Entities.Remove(entity);
  }

  public void Tick()
  {
    var entities = (ushort) _entitiesDirtySet.Count;
    if (entities > 0)
    {
      _writer.Clear();
      _writer.WriteOperation(RagonOperation.REPLICATE_ENTITY_STATE);
      _writer.WriteUShort(entities);

      foreach (var entity in _entitiesDirtySet)
        entity.State.Write(_writer);

      _entitiesDirtySet.Clear();

      var sendData = _writer.ToArray();
      foreach (var roomPlayer in ReadyPlayersList)
        roomPlayer.Connection.UnreliableChannel.Send(sendData);
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
        _writer.Clear();
        _writer.WriteOperation(RagonOperation.PLAYER_LEAVED);
        _writer.WriteString(player.Id);

        var entitiesToDelete = player.Entities.DynamicList;
        _writer.WriteUShort((ushort) entitiesToDelete.Count);
        foreach (var entity in entitiesToDelete)
        {
          _writer.WriteUShort(entity.Id);
          EntityList.Remove(entity);
        }

        var sendData = _writer.ToArray();
        Broadcast(sendData);
      }
      
      if (roomPlayer.Connection.Id == Owner.Connection.Id && PlayerList.Count > 0)
      {
        var nextOwner = PlayerList[0];
        
        Owner = nextOwner;
        
        var entitiesToUpdate =  roomPlayer.Entities.StaticList;
        
        _writer.Clear();
        _writer.WriteOperation(RagonOperation.OWNERSHIP_CHANGED);
        _writer.WriteString(Owner.Id);
        _writer.WriteUShort((ushort) entitiesToUpdate.Count);
      
        foreach (var entity in entitiesToUpdate)
        {
          _writer.WriteUShort(entity.Id);
        
          entity.SetOwner(nextOwner);
          nextOwner.Entities.Add(entity);
        }

        var sendData = _writer.ToArray();
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
      readyPlayer.Connection.ReliableChannel.Send(data);
  }
}