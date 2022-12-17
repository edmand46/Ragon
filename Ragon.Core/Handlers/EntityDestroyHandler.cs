using NLog;
using Ragon.Common;

namespace Ragon.Core.Handlers;

public sealed class EntityDestroyHandler: IHandler
{
  private Logger _logger = LogManager.GetCurrentClassLogger();
  public void Handle(PlayerContext context, RagonSerializer reader, RagonSerializer writer)
  {
    var entityId = reader.ReadUShort();
    if (context.Room.Entities.TryGetValue(entityId, out var entity))
    {
      var player = context.RoomPlayer;
      var payload = reader.ReadData(reader.Size);
      
      context.Room.DetachEntity(player, entity, Array.Empty<byte>());
      _logger.Trace($"Player {context.Connection.Id}|{context.LobbyPlayer.Name} destoyed entity {entity.Id}");
    }
  }
}