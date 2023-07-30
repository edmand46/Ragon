/*
 * Copyright 2023 Eduard Kargin <kargin.eduard@gmail.com>
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */


using Ragon.Protocol;
using Ragon.Server.Room;

namespace Ragon.Server.Entity;

public class RagonEntity : IRagonEntity
{
  private static ushort _idGenerator = 100;
  public ushort Id { get; private set; }
  public ushort Type { get; private set; }
  public ushort StaticId { get; private set; }
  public ushort AttachId { get; private set; }
  public RagonRoomPlayer Owner { get; private set; }
  public RagonAuthority Authority { get; private set; }
  public RagonPayload Payload { get; private set; }
  public IRagonEntityState State => _state;

  private readonly List<RagonEvent> _bufferedEvents;
  private readonly int _limitBufferedEvents;
  private readonly RagonEntityState _state;

  public RagonEntity(RagonEntityParameters parameters)
  {
    Id = _idGenerator++;

    StaticId = parameters.StaticId;
    Type = parameters.Type;
    AttachId = parameters.AttachId;
    Authority = parameters.Authority;
    Payload = new RagonPayload();

    _state = new RagonEntityState(this);
    _bufferedEvents = new List<RagonEvent>();
    _limitBufferedEvents = parameters.BufferedEvents;
  }

  public void Attach(RagonRoomPlayer owner)
  {
    Owner = owner;
  }

  public void Detach()
  {
  }

  public void RestoreBufferedEvents(RagonRoomPlayer roomPlayer, RagonBuffer writer)
  {
    foreach (var evnt in _bufferedEvents)
    {
      writer.Clear();
      writer.WriteOperation(RagonOperation.REPLICATE_ENTITY_EVENT);
      writer.WriteUShort(evnt.EventCode);
      writer.WriteUShort(evnt.Invoker.Connection.Id);
      writer.WriteByte((byte)RagonReplicationMode.Server);
      writer.WriteUShort(Id);

      evnt.Write(writer);

      var sendData = writer.ToArray();
      roomPlayer.Connection.Reliable.Send(sendData);
    }
  }

  public void Create()
  {
    var room = Owner.Room;
    var buffer = room.Writer;

    buffer.Clear();
    buffer.WriteOperation(RagonOperation.CREATE_ENTITY);
    buffer.WriteUShort(AttachId);
    buffer.WriteUShort(Type);
    buffer.WriteUShort(Id);
    buffer.WriteUShort(Owner.Connection.Id);

    Payload.Write(buffer);

    var sendData = buffer.ToArray();
    foreach (var player in room.ReadyPlayersList)
      player.Connection.Reliable.Send(sendData);
  }

  public void Destroy()
  {
    var room = Owner.Room;
    var buffer = room.Writer;

    buffer.Clear();
    buffer.WriteOperation(RagonOperation.REMOVE_ENTITY);
    buffer.WriteUShort(Id);

    Payload.Write(buffer);

    var sendData = buffer.ToArray();
    foreach (var player in room.ReadyPlayersList)
      player.Connection.Reliable.Send(sendData);
  }

  public void Snapshot(RagonBuffer buffer)
  {
    buffer.WriteUShort(Type);
    buffer.WriteUShort(Id);
    
    if (StaticId != 0)
      buffer.WriteUShort(StaticId);
    
    buffer.WriteUShort(Owner.Connection.Id);
    buffer.WriteUShort(Payload.Size);
    
    Payload.Write(buffer);
    
    _state.Snapshot(buffer);
  }

  public void ReplicateEvent(
    RagonRoomPlayer caller,
    RagonEvent evnt,
    RagonReplicationMode eventMode,
    RagonRoomPlayer targetPlayer
  )
  {
    var room = Owner.Room;
    var buffer = room.Writer;

    buffer.Clear();
    buffer.WriteOperation(RagonOperation.REPLICATE_ENTITY_EVENT);
    buffer.WriteUShort(evnt.EventCode);
    buffer.WriteUShort(caller.Connection.Id);
    buffer.WriteByte((byte)eventMode);
    buffer.WriteUShort(Id);

    evnt.Write(buffer);

    var sendData = buffer.ToArray();
    targetPlayer.Connection.Reliable.Send(sendData);
  }

  public void ReplicateEvent(
    RagonRoomPlayer caller,
    RagonEvent evnt,
    RagonReplicationMode eventMode,
    RagonTarget targetMode
  )
  {
    if (Authority == RagonAuthority.OwnerOnly &&
        Owner.Connection.Id != caller.Connection.Id)
    {
      Console.WriteLine($"Player have not enough authority for event with Id {evnt.EventCode}");
      return;
    }

    if (eventMode == RagonReplicationMode.Buffered &&
        targetMode != RagonTarget.Owner &&
        _bufferedEvents.Count < _limitBufferedEvents)
    {
      _bufferedEvents.Add(evnt);
    }

    var room = Owner.Room;
    var buffer = room.Writer;

    buffer.Clear();
    buffer.WriteOperation(RagonOperation.REPLICATE_ENTITY_EVENT);
    buffer.WriteUShort(evnt.EventCode);
    buffer.WriteUShort(caller.Connection.Id);
    buffer.WriteByte((byte)eventMode);
    buffer.WriteUShort(Id);

    evnt.Write(buffer);

    var sendData = buffer.ToArray();
    switch (targetMode)
    {
      case RagonTarget.Owner:
      {
        Owner.Connection.Reliable.Send(sendData);
        break;
      }
      case RagonTarget.ExceptOwner:
      {
        foreach (var roomPlayer in room.ReadyPlayersList)
        {
          if (roomPlayer.Connection.Id != Owner.Connection.Id)
            roomPlayer.Connection.Reliable.Send(sendData);
        }

        break;
      }
      case RagonTarget.ExceptInvoker:
      {
        foreach (var roomPlayer in room.ReadyPlayersList)
        {
          if (roomPlayer.Connection.Id != caller.Connection.Id)
            roomPlayer.Connection.Reliable.Send(sendData);
        }

        break;
      }
      case RagonTarget.All:
      {
        foreach (var roomPlayer in room.ReadyPlayersList)
          roomPlayer.Connection.Reliable.Send(sendData);
        break;
      }
    }
  }

  public void AddProperty(RagonProperty property)
  {
    _state.AddProperty(property);
  }

  public void WriteState(RagonBuffer writer)
  {
    _state.Write(writer);
  }

  public bool TryReadState(RagonRoomPlayer player, RagonBuffer reader)
  {
    if (Owner.Connection.Id != player.Connection.Id)
      return false;

    _state.Read(reader);

    return true;
  }
}