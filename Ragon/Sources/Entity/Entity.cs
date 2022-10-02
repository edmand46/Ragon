using System;
using System.Collections.Generic;
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
  public List<EntityEvent> BufferedEvents = new List<EntityEvent>();

  public byte[] Payload { get; set; }
  
  public Entity(ushort ownerId, ushort entityType, ushort staticId, RagonAuthority eventAuthority, int props)
  {
    OwnerId = ownerId;
    StaticId = staticId;
    EntityType = entityType;
    EntityId = _idGenerator++;
    Properties = new EntityProperty[props];
    Payload = Array.Empty<byte>();
    Authority = eventAuthority;
  }

  public void UpdateOwner(ushort ownerId) => OwnerId = ownerId;

  public void ReplicateProperties(RagonSerializer serializer)
  {
    serializer.WriteUShort(EntityId);

    for (int propertyIndex = 0; propertyIndex < Properties.Length; propertyIndex++)
    {
      var property = Properties[propertyIndex];
      if (property.IsDirty)
      {
        serializer.WriteBool(true);
        var span = serializer.GetWritableData(property.Size);
        var data = property.Read();
        data.CopyTo(span);
        property.Clear();
      }
      else
      {
        serializer.WriteBool(false);
      }
    }
  }
  
  public void Snapshot(RagonSerializer serializer)
  {
    for (int propertyIndex = 0; propertyIndex < Properties.Length; propertyIndex++)
    {
      var property = Properties[propertyIndex];
      var hasPayload = property.IsFixed || property.Size > 0 && !property.IsFixed;
      if (hasPayload)
      {
        serializer.WriteBool(true);
        var span = serializer.GetWritableData(property.Size);
        var data = property.Read();
        data.CopyTo(span);
      }
      else
      {
        serializer.WriteBool(false);
      }
    }
  }
  
}