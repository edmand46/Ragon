using Ragon.Common;

namespace Ragon.Core.Game;

public class Entity
{
  private static ushort _idGenerator = 0;

  public ushort Id { get; private set; }
  public RoomPlayer Owner { get; private set; }
  public RagonAuthority Authority { get; private set; }
  public EntityState State { get; private set; }
  public byte[] Payload { get; private set; }
  public ushort StaticId { get; private set; }
  public ushort Type { get; private set; }
  
  private List<EntityEvent> _bufferedEvents;

  public Entity(RoomPlayer owner, ushort type, ushort staticId, RagonAuthority eventAuthority)
  {
    Owner = owner;
    StaticId = staticId;
    Type = type;
    Id = _idGenerator++;
    Payload = Array.Empty<byte>();
    Authority = eventAuthority;
    State = new EntityState(this);
    
    _bufferedEvents = new List<EntityEvent>();
  }

  public void SetPayload(byte[] payload)
  {
    Payload = payload;
  }

  public void SetOwner(RoomPlayer owner)
  {
    Owner = owner;
  }
  
  public void RestoreBufferedEvents(RoomPlayer roomPlayer, RagonSerializer writer)
  {
    foreach (var bufferedEvent in _bufferedEvents)
    {
      writer.Clear();
      writer.WriteOperation(RagonOperation.REPLICATE_ENTITY_EVENT);
      writer.WriteUShort(bufferedEvent.EventId);
      writer.WriteUShort(bufferedEvent.PeerId);
      writer.WriteByte((byte) RagonReplicationMode.Server);
      writer.WriteUShort(Id);

      ReadOnlySpan<byte> data = bufferedEvent.EventData.AsSpan();
      writer.WriteData(ref data);

      var sendData = writer.ToArray();
      roomPlayer.Connection.ReliableChannel.Send(sendData);
    }
  }

  public void Create()
  {
    var room = Owner.Room;
    var serializer = room.Writer;

    serializer.Clear();
    serializer.WriteOperation(RagonOperation.CREATE_ENTITY);
    serializer.WriteUShort(Type);
    serializer.WriteUShort(Id);
    serializer.WriteUShort(Owner.Connection.Id);

    ReadOnlySpan<byte> entityPayload = Payload.AsSpan();
    serializer.WriteUShort((ushort) entityPayload.Length);
    serializer.WriteData(ref entityPayload);

    var sendData = serializer.ToArray();
    foreach (var player in room.ReadyPlayersList)
      player.Connection.ReliableChannel.Send(sendData);
  }

  public void Destroy(byte[] payload)
  {
    var room = Owner.Room;
    var serializer = room.Writer;

    serializer.Clear();
    serializer.WriteOperation(RagonOperation.DESTROY_ENTITY);
    serializer.WriteInt(Id);
    serializer.WriteUShort(0);
    // serializer.WriteData(ref Payload);

    var sendData = serializer.ToArray();
    foreach (var player in room.ReadyPlayersList)
      player.Connection.ReliableChannel.Send(sendData);
  }

  public void ReplicateEvent(
    RoomPlayer caller,
    ushort eventId,
    ReadOnlySpan<byte> payload,
    RagonReplicationMode eventMode,
    RoomPlayer targetPlayer
  )
  {
    var room = Owner.Room;
    var serializer = room.Writer;

    serializer.Clear();
    serializer.WriteOperation(RagonOperation.REPLICATE_ENTITY_EVENT);
    serializer.WriteUShort(eventId);
    serializer.WriteUShort(caller.Connection.Id);
    serializer.WriteByte((byte) eventMode);
    serializer.WriteUShort(Id);
    serializer.WriteData(ref payload);

    var sendData = serializer.ToArray();
    targetPlayer.Connection.ReliableChannel.Send(sendData);
  }

  public void ReplicateEvent(
    RoomPlayer caller,
    ushort eventId,
    ReadOnlySpan<byte> payload,
    RagonReplicationMode eventMode,
    RagonTarget targetMode
  )
  {
    if (Authority == RagonAuthority.OwnerOnly &&
        Owner.Connection.Id != caller.Connection.Id)
    {
      Console.WriteLine($"Player have not enought authority for event with Id {eventId}");
      return;
    }

    if (eventMode == RagonReplicationMode.Buffered && targetMode != RagonTarget.Owner)
    {
      var bufferedEvent = new EntityEvent()
      {
        EventData = payload.ToArray(),
        Target = targetMode,
        EventId = eventId,
        PeerId = caller.Connection.Id,
      };
      _bufferedEvents.Add(bufferedEvent);
    }

    var room = Owner.Room;
    var serializer = room.Writer;

    serializer.Clear();
    serializer.WriteOperation(RagonOperation.REPLICATE_ENTITY_EVENT);
    serializer.WriteUShort(eventId);
    serializer.WriteUShort(caller.Connection.Id);
    serializer.WriteByte((byte) eventMode);
    serializer.WriteUShort(Id);
    serializer.WriteData(ref payload);

    var sendData = serializer.ToArray();

    switch (targetMode)
    {
      case RagonTarget.Owner:
      {
        Owner.Connection.ReliableChannel.Send(sendData);
        break;
      }
      case RagonTarget.ExceptOwner:
      {
        foreach (var roomPlayer in room.ReadyPlayersList)
        {
          if (roomPlayer.Connection.Id != Owner.Connection.Id)
            roomPlayer.Connection.ReliableChannel.Send(sendData);
        }

        break;
      }
      case RagonTarget.ExceptInvoker:
      {
        foreach (var roomPlayer in room.ReadyPlayersList)
        {
          if (roomPlayer.Connection.Id != caller.Connection.Id)
            roomPlayer.Connection.ReliableChannel.Send(sendData);
        }

        break;
      }
      case RagonTarget.All:
      {
        foreach (var roomPlayer in room.ReadyPlayersList)
          roomPlayer.Connection.ReliableChannel.Send(sendData);
        break;
      }
    }
  }
}