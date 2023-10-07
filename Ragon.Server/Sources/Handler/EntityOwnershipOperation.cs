using NLog;
using Ragon.Protocol;

namespace Ragon.Server.Handler;

public sealed class EntityOwnershipOperation : BaseOperation
{
  private readonly Logger _logger = LogManager.GetCurrentClassLogger();
  
  public EntityOwnershipOperation(RagonBuffer reader, RagonBuffer writer) : base(reader, writer)
  {
  }

  public override void Handle(RagonContext context, byte[] data)
  {
    var currentOwner = context.RoomPlayer;
    var room = context.Room;
    
    var entityId = Reader.ReadUShort();
    var playerPeerId = Reader.ReadUShort();

    if (!room.Entities.TryGetValue(entityId, out var entity))
    {
      _logger.Error($"Entity not found with id {entityId}");
      return;
    }
    
    if (entity.Owner.Connection.Id != currentOwner.Connection.Id)
    {
      _logger.Error($"Player not owner of entity with id {entityId}");
      return;
    } 

    if (!room.Players.TryGetValue(playerPeerId, out var nextOwner))
    {
      _logger.Error($"Player not found with id {playerPeerId}");
      return;
    }    
    
    currentOwner.Entities.Remove(entity);
    nextOwner.Entities.Add(entity);
    
    entity.Attach(nextOwner);
    
    _logger.Trace($"Entity {entity.Id} next owner {nextOwner.Connection.Id}");
    
    Writer.Clear();
    Writer.WriteOperation(RagonOperation.OWNERSHIP_ENTITY_CHANGED);
    Writer.WriteUShort(playerPeerId);
    Writer.WriteUShort(1);
    Writer.WriteUShort(entity.Id);
    
    var sendData = Writer.ToArray();
    foreach (var player in room.PlayerList)
      player.Connection.Reliable.Send(sendData);
  }
}