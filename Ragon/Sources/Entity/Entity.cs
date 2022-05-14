using Ragon.Common;

namespace Ragon.Core;

public class Entity
{
  private static int _idGenerator = 0;
  public int EntityId { get; private set; }
  public uint OwnerId { get; private set; }
  public ushort EntityType { get; private set; }
  public RagonAuthority Authority { get; private set; }
  public EntityState State { get; private set; }
  
  public Entity(uint ownerId, ushort entityType, RagonAuthority stateAuthority, RagonAuthority eventAuthority)
  {
    OwnerId = ownerId;
    EntityType = entityType;
    EntityId = _idGenerator++;
    State = new EntityState(stateAuthority);
    Authority = eventAuthority;
  }
}