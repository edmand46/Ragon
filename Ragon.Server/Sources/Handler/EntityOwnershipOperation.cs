using NLog;
using Ragon.Protocol;

namespace Ragon.Server.Handler;

public sealed class EntityOwnershipOperation : IRagonOperation
{
  private readonly Logger _logger = LogManager.GetCurrentClassLogger();

  public void Handle(RagonContext context, RagonBuffer reader, RagonBuffer writer)
  {
    var currentOwner = context.RoomPlayer;
    var room = context.Room;
    
    var entityId = reader.ReadUShort();
    var playerPeerId = reader.ReadUShort();

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
    
    writer.Clear();
    writer.WriteOperation(RagonOperation.OWNERSHIP_ENTITY_CHANGED);
    writer.WriteUShort(playerPeerId);
    writer.WriteUShort(1);
    writer.WriteUShort(entity.Id);
    
    var sendData = writer.ToArray();
    foreach (var player in room.PlayerList)
      player.Connection.Reliable.Send(sendData);
  }
}