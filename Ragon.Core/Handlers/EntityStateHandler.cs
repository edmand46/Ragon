using NLog;
using Ragon.Common;

namespace Ragon.Core.Handlers;

public sealed class EntityStateHandler: IHandler
{
  private ILogger _logger = LogManager.GetCurrentClassLogger();
  
  public void Handle(PlayerContext context, RagonSerializer reader, RagonSerializer writer)
  {
    var room = context.Room;
    var entitiesCount = reader.ReadUShort();
    for (var entityIndex = 0; entityIndex < entitiesCount; entityIndex++)
    {
      var entityId = reader.ReadUShort();
      
      if (room.Entities.TryGetValue(entityId, out var entity))
      {
        entity.State.Read(reader);
        room.Track(entity);
      }
      else
      {
        _logger.Error($"Entity with Id {entityId} not found, replication interrupted");
      }
    }
  }
}