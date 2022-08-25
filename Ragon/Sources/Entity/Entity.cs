using System;
using Ragon.Common;

namespace Ragon.Core;

public class Entity
{
  private static ushort _idGenerator = 0;
  public ushort EntityId { get; private set; }
  public ushort StaticId { get; private set; }
  public ushort EntityType { get; private set; }
  public ushort OwnerId { get; private set; }
  public RagonAuthority Authority { get; private set; }
  public EntityProperty[] Properties { get; private set; }
  public byte[] Payload { get; set; }
  
  public Entity(ushort ownerId, ushort entityType, ushort staticId, RagonAuthority stateAuthority, RagonAuthority eventAuthority, int props)
  {
    OwnerId = ownerId;
    StaticId = staticId;
    EntityType = entityType;
    EntityId = _idGenerator++;
    Properties = new EntityProperty[props];
    Payload = Array.Empty<byte>();
    Authority = eventAuthority;
  }
}