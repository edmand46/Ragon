using NLog;
using Ragon.Protocol;

namespace Ragon.Server.Handler;

public sealed class EntityOwnershipOperation : IRagonOperation
{
  private readonly Logger _logger = LogManager.GetCurrentClassLogger();

  public void Handle(RagonContext context, RagonBuffer reader, RagonBuffer writer)
  {
    var player = context.RoomPlayer;
    var room = context.Room;
    
    var entityId = reader.ReadUShort();
    var playerId = reader.ReadUShort();

    if (!room.Entities.TryGetValue(entityId, out var entity))
    {
      _logger.Error($"Entity not found with id {entityId}");
      return;
    }
    
    if (entity.Owner.Connection.Id != player.Connection.Id)
    {
      _logger.Error($"Player not owner of entity with id {entityId}");
      return;
    } 

    if (!room.Players.TryGetValue(playerId, out var nextOwner))
    {
      _logger.Error($"Player not found with id {entityId}");
      return;
    }    
    
    writer.Clear();
    writer.WriteOperation(RagonOperation.OWNERSHIP_ENTITY_CHANGED);
    writer.WriteUShort(nextOwner.Connection.Id);
    writer.WriteUShort(1);
    writer.WriteUShort(entity.Id);

    player.Entities.Remove(entity);
    nextOwner.Entities.Add(entity);
    
    entity.Attach(nextOwner);
    
    _logger.Trace($"Entity {entity.Id} next owner {nextOwner.Connection.Id}");
  }
}