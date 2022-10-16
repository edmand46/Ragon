using System;
using System.Collections.Generic;
using NLog;
using Ragon.Common;

namespace Ragon.Core;

public class Entity
{
  private ILogger _logger = LogManager.GetCurrentClassLogger();
  private GameRoom _room;

  private static ushort _idGenerator = 0;
  public ushort EntityId { get; private set; }
  public ushort StaticId { get; private set; }
  public ushort EntityType { get; private set; }
  public ushort OwnerId { get; private set; }
  public byte[] Payload { get; private set; }
  public RagonAuthority Authority { get; private set; }

  private List<EntityProperty> _properties;
  private List<EntityEvent> _bufferedEvents;

  public Entity(GameRoom room, ushort ownerId, ushort entityType, ushort staticId, RagonAuthority eventAuthority)
  {
    OwnerId = ownerId;
    StaticId = staticId;
    EntityType = entityType;
    EntityId = _idGenerator++;
    Payload = Array.Empty<byte>();
    Authority = eventAuthority;

    _room = room;
    _properties = new List<EntityProperty>();
    _bufferedEvents = new List<EntityEvent>();
  }

  public void SetPayload(byte[] payload)
  {
    Payload = payload;
  }
  
  public void SetOwner(ushort ownerId)
  {
    OwnerId = ownerId;
  }

  public void AddProperty(EntityProperty property)
  {
    _properties.Add(property);
  }
  
  public void ReplicateEvent(ushort peerId, ushort eventId, ReadOnlySpan<byte> payload, RagonReplicationMode eventMode, RagonTarget targetMode)
  {
    if (Authority == RagonAuthority.OwnerOnly && OwnerId != peerId)
    {
      _logger.Warn($"Player have not enought authority for event with Id {eventId}");
      return;
    }

    if (eventMode == RagonReplicationMode.Buffered && targetMode != RagonTarget.Owner)
    {
      var bufferedEvent = new EntityEvent()
      {
        EventData = payload.ToArray(),
        Target = targetMode,
        EventId = eventId,
        PeerId = peerId,
      };
      _bufferedEvents.Add(bufferedEvent);
    }

    var serializer = _room.GetSharedSerializer();

    serializer.Clear();
    serializer.WriteOperation(RagonOperation.REPLICATE_ENTITY_EVENT);
    serializer.WriteUShort(eventId);
    serializer.WriteUShort(peerId);
    serializer.WriteByte((byte) eventMode);
    serializer.WriteUShort(EntityId);
    serializer.WriteData(ref payload);

    var sendData = serializer.ToArray();
    Send(targetMode, sendData);
  }

  public void ReadState(uint peerId, RagonSerializer serializer)
  {
    if (OwnerId != peerId)
    {
      _logger.Warn($"Not owner can't change properties of object {EntityId}");
      return;
    }

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
          _logger.Warn($"Property {i} payload too large, size: {size}");
          continue;
        }

        var propertyPayload = serializer.ReadData(size);
        property.Write(ref propertyPayload);
        property.Size = size;
      }
    }
  }

  public void WriteProperties(RagonSerializer serializer)
  {
    serializer.WriteUShort(EntityId);

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

  public void WriteSnapshot(RagonSerializer serializer)
  {
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

  public void RestoreBufferedEvents(ushort peerId)
  {
    var serializer = _room.GetSharedSerializer();
    foreach (var bufferedEvent in _bufferedEvents)
    {
      serializer.Clear();
      serializer.WriteOperation(RagonOperation.REPLICATE_ENTITY_EVENT);
      serializer.WriteUShort(bufferedEvent.EventId);
      serializer.WriteUShort(bufferedEvent.PeerId);
      serializer.WriteByte((byte) RagonReplicationMode.Server);
      serializer.WriteUShort(EntityId);

      ReadOnlySpan<byte> data = bufferedEvent.EventData.AsSpan();
      serializer.WriteData(ref data);

      var sendData = serializer.ToArray();
      _room.Send(peerId, sendData, DeliveryType.Reliable);
    }
  }

  public void Create()
  {
    var serializer = _room.GetSharedSerializer();

    serializer.Clear();
    serializer.WriteOperation(RagonOperation.CREATE_ENTITY);
    serializer.WriteUShort(EntityType);
    serializer.WriteUShort(EntityId);
    serializer.WriteUShort(OwnerId);

    ReadOnlySpan<byte> entityPayload = Payload.AsSpan();
    serializer.WriteUShort((ushort) entityPayload.Length);
    serializer.WriteData(ref entityPayload);

    var sendData = serializer.ToArray();
    _room.BroadcastToReady(sendData, DeliveryType.Reliable);
  }

  public void Destroy(ReadOnlySpan<byte> payload)
  {
    var serializer = _room.GetSharedSerializer();
    serializer.Clear();
    serializer.WriteOperation(RagonOperation.DESTROY_ENTITY);
    serializer.WriteInt(EntityId);
    serializer.WriteUShort((ushort) payload.Length);
    serializer.WriteData(ref payload);
    
    var sendData = serializer.ToArray();
    _room.BroadcastToReady(sendData, DeliveryType.Reliable);
  }

  void Send(RagonTarget targetMode, byte[] sendData)
  {
    switch (targetMode)
    {
      case RagonTarget.Owner:
      {
        _room.Send(OwnerId, sendData, DeliveryType.Reliable);
        break;
      }
      case RagonTarget.ExceptOwner:
      {
        _room.BroadcastToReady(sendData, new [] { OwnerId }, DeliveryType.Reliable);
        break;
      }
      case RagonTarget.All:
      {
        _room.BroadcastToReady(sendData, DeliveryType.Reliable);
        break;
      }
    }
  }
}