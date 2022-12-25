using NLog;
using Ragon.Common;

namespace Ragon.Core.Game;

public class  EntityState
{
  private List<EntityStateProperty> _properties;
  private Entity _entity;

  public EntityState(Entity entity, int capacity = 10)
  {
    _entity = entity;
    _properties = new List<EntityStateProperty>(10);
  }

  public void AddProperty(EntityStateProperty property)
  {
    _properties.Add(property);
  }
  
  public void Write(RagonSerializer serializer)
  {
    serializer.WriteUShort(_entity.Id);
 
    for (int propertyIndex = 0; propertyIndex < _properties.Count; propertyIndex++)
    {
      var property = _properties[propertyIndex];
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
  
  public void Read(RagonSerializer serializer)
  {
    for (var i = 0; i < _properties.Count; i++)
    {
      if (serializer.ReadBool())
      {
        var property = _properties[i];
        var size = property.Size;
        if (!property.IsFixed)
          size = serializer.ReadUShort();
        
        if (size > property.Capacity)
        {
          Console.WriteLine($"Property {i} payload too large, size: {size}");
          continue;
        }

        var propertyPayload = serializer.ReadData(size);
        property.Write(ref propertyPayload);
        property.Size = size;
      }
    }
  }
  
  public void Snapshot(RagonSerializer serializer)
  {
    ReadOnlySpan<byte> payload = _entity.Payload.AsSpan();

    serializer.WriteUShort(_entity.Type);
    serializer.WriteUShort(_entity.Id);
    
    if (_entity.StaticId != 0)
      serializer.WriteUShort(_entity.StaticId);
    
    serializer.WriteUShort(_entity.Owner.Connection.Id);
    serializer.WriteUShort((ushort) payload.Length);
    serializer.WriteData(ref payload);
    
    for (int propertyIndex = 0; propertyIndex < _properties.Count; propertyIndex++)
    {
      var property = _properties[propertyIndex];
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