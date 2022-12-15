using NLog;
using Ragon.Common;
using Ragon.Core.Game;


namespace Ragon.Core.Handlers;

public sealed class EntityCreateHandler: IHandler
{
  private Logger _logger = LogManager.GetCurrentClassLogger();
  public void Handle(PlayerContext context, RagonSerializer reader, RagonSerializer writer)
  {
    var entityType = reader.ReadUShort();
    var eventAuthority = (RagonAuthority) reader.ReadByte();
    var propertiesCount = reader.ReadUShort();
          
    var entity = new Entity(context.RoomPlayer, entityType, 0, eventAuthority);
    for (var i = 0; i < propertiesCount; i++)
    {
      var propertyType = reader.ReadBool();
      var propertySize = reader.ReadUShort();
      entity.State.AddProperty(new EntityStateProperty(propertySize, propertyType));
    }
    
    var entityPayload = reader.ReadData(reader.Size);
    entity.SetPayload(entityPayload.ToArray());
    
    context.Room.AttachEntity(context.RoomPlayer, entity);
    
    _logger.Trace($"Player {context.Connection.Id}|{context.LobbyPlayer.Name} created entity {entity.Id}:{entity.Type}");
  }
}