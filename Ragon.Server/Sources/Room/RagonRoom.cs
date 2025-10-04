/*
 * Copyright 2023-2024 Eduard Kargin <kargin.eduard@gmail.com>
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
using Ragon.Server.Data;
using Ragon.Server.Entity;
using Ragon.Server.Event;
using Ragon.Server.IO;
using Ragon.Server.Plugin;
using Ragon.Server.Time;

namespace Ragon.Server.Room;

public class RagonRoom : IRagonRoom, IRagonAction
{
  public string Id { get; private set; }
  public string Scene { get; private set; }
  public int PlayerMax { get; private set; }
  public int PlayerMin { get; private set; }
  public int PlayerCount => WaitPlayersList.Count;
  public int ProjectId { get; private set; }

  public bool IsDone { get; private set; }

  public RagonData UserData { get; set; }
  public RagonRoomPlayer Owner { get; private set; }
  public RagonBuffer Writer { get; }
  public IRoomPlugin Plugin { get; private set; }

  public Dictionary<ushort, RagonRoomPlayer> Players { get; private set; }
  public List<RagonRoomPlayer> WaitPlayersList { get; private set; }
  public List<RagonRoomPlayer> ReadyPlayersList { get; private set; }
  public List<RagonRoomPlayer> PlayerList { get; private set; }

  public Dictionary<ushort, RagonEntity> Entities { get; private set; }
  public List<RagonEntity> DynamicEntitiesList { get; private set; }
  public List<RagonEntity> StaticEntitiesList { get; private set; }
  public List<RagonEntity> EntityList { get; private set; }

  private readonly HashSet<RagonEntity> _entitiesDirtySet;
  private readonly List<RagonEvent> _bufferedEvents;
  private readonly int _limitBufferedEvents;

  public RagonRoom(string roomId, RoomInformation info, IRoomPlugin roomPlugin, int projectId)
  {
    Id = roomId;
    Scene = info.Scene;
    PlayerMax = info.Max;
    PlayerMin = info.Min;
    Plugin = roomPlugin;
    ProjectId = projectId;

    Players = new Dictionary<ushort, RagonRoomPlayer>(info.Max);
    WaitPlayersList = new List<RagonRoomPlayer>(info.Max);
    ReadyPlayersList = new List<RagonRoomPlayer>(info.Max);
    PlayerList = new List<RagonRoomPlayer>(info.Max);

    Entities = new Dictionary<ushort, RagonEntity>();
    DynamicEntitiesList = new List<RagonEntity>();
    StaticEntitiesList = new List<RagonEntity>();
    EntityList = new List<RagonEntity>();

    _entitiesDirtySet = new HashSet<RagonEntity>();
    _bufferedEvents = new List<RagonEvent>();
    _limitBufferedEvents = 1000;

    UserData = new RagonData();
    Writer = new RagonBuffer();
  }

  public void AttachEntity(RagonEntity entity)
  {
    Entities.Add(entity.Id, entity);
    EntityList.Add(entity);

    if (entity.StaticId == 0)
      DynamicEntitiesList.Add(entity);
    else
      StaticEntitiesList.Add(entity);
  }

  public void DetachEntity(RagonEntity entity)
  {
    Entities.Remove(entity.Id);
    EntityList.Remove(entity);
    StaticEntitiesList.Remove(entity);
    DynamicEntitiesList.Remove(entity);

    _entitiesDirtySet.Remove(entity);
  }

  public void RestoreBufferedEvents(RagonRoomPlayer roomPlayer)
  {
    foreach (var evnt in _bufferedEvents)
    {
      Writer.Clear();
      Writer.WriteOperation(RagonOperation.REPLICATE_ROOM_EVENT);
      Writer.WriteUShort(evnt.EventCode);
      Writer.WriteUShort(evnt.Invoker.Connection.Id);
      Writer.WriteByte((byte)RagonReplicationMode.Server);

      evnt.Write(Writer);

      var sendData = Writer.ToArray();
      roomPlayer.Connection.Reliable.Send(sendData);
    }
  }

  public void ReplicateEvent(
    RagonRoomPlayer invoker,
    RagonEvent evnt,
    RagonReplicationMode eventMode,
    RagonRoomPlayer targetPlayer
  )
  {
    var room = Owner.Room;
    var buffer = room.Writer;

    buffer.Clear();
    buffer.WriteOperation(RagonOperation.REPLICATE_ROOM_EVENT);
    buffer.WriteUShort(evnt.EventCode);
    buffer.WriteUShort(invoker.Connection.Id);
    buffer.WriteByte((byte)eventMode);

    evnt.Write(buffer);

    var sendData = buffer.ToArray();
    targetPlayer.Connection.Reliable.Send(sendData);
  }

  public void ReplicateEvent(
    RagonRoomPlayer invoker,
    RagonEvent evnt,
    RagonReplicationMode eventMode,
    RagonTarget targetMode
  )
  {
    if (eventMode == RagonReplicationMode.Buffered && targetMode != RagonTarget.Owner &&
        _bufferedEvents.Count < _limitBufferedEvents)
    {
      _bufferedEvents.Add(evnt);
    }

    Writer.Clear();
    Writer.WriteOperation(RagonOperation.REPLICATE_ROOM_EVENT);
    Writer.WriteUShort(evnt.EventCode);
    Writer.WriteUShort(invoker.Connection.Id);
    Writer.WriteByte((byte)eventMode);

    evnt.Write(Writer);

    var sendData = Writer.ToArray();
    switch (targetMode)
    {
      case RagonTarget.Owner:
      {
        Owner.Connection.Reliable.Send(sendData);
        break;
      }
      case RagonTarget.ExceptOwner:
      {
        foreach (var roomPlayer in ReadyPlayersList)
        {
          if (roomPlayer.Connection.Id != Owner.Connection.Id)
            roomPlayer.Connection.Reliable.Send(sendData);
        }

        break;
      }
      case RagonTarget.ExceptInvoker:
      {
        foreach (var roomPlayer in ReadyPlayersList)
        {
          if (roomPlayer.Connection.Id != invoker.Connection.Id)
            roomPlayer.Connection.Reliable.Send(sendData);
        }

        break;
      }
      case RagonTarget.All:
      {
        Broadcast(sendData);
        break;
      }
    }
  }
  public void ReplicateData(byte[] data, NetworkChannel channel)
  {
    ReplicateData(data, ReadyPlayersList, channel);
  }

  public void ReplicateData(byte[] data, List<RagonRoomPlayer> receivers,
    NetworkChannel channel = NetworkChannel.RELIABLE)
  {
    var dataSize = data.Length;
    var headerSize = 3;
    var size = headerSize + dataSize;
    var sendData = new byte[size];
    var peerId = 10000; // Server Peer

    sendData[0] = (byte)RagonOperation.REPLICATE_RAW_DATA;
    sendData[1] = (byte)peerId;
    sendData[2] = (byte)(peerId >> 8);

    Array.Copy(data, 0, sendData, headerSize, dataSize);

    Broadcast(sendData, receivers, channel);
  }
  
  public void ReplicateData(RagonRoomPlayer invoker, byte[] data, List<RagonRoomPlayer> receivers,
    NetworkChannel channel = NetworkChannel.RELIABLE)
  {
    var dataSize = data.Length;
    var headerSize = 3;
    var size = headerSize + dataSize;
    var sendData = new byte[size];
    var peerId = invoker.Connection.Id;

    sendData[0] = (byte)RagonOperation.REPLICATE_RAW_DATA;
    sendData[1] = (byte)peerId;
    sendData[2] = (byte)(peerId >> 8);

    Array.Copy(data, 0, sendData, headerSize, dataSize);

    Broadcast(sendData, receivers, channel);
  }

  public void Tick(float dt)
  {
    var entities = (ushort)_entitiesDirtySet.Count;
    if (entities > 0)
    {
      Writer.Clear();
      Writer.WriteOperation(RagonOperation.REPLICATE_ENTITY_STATE);
      Writer.WriteUShort(entities);

      foreach (var entity in _entitiesDirtySet)
        entity.WriteState(Writer);

      _entitiesDirtySet.Clear();

      var sendData = Writer.ToArray();
      foreach (var roomPlayer in ReadyPlayersList)
        roomPlayer.Connection.Unreliable.Send(sendData);
    }
  }

  public void AttachPlayer(RagonRoomPlayer player)
  {
    if (Players.Count == 0)
      Owner = player;

    player.OnAttached(this);

    PlayerList.Add(player);
    Players.Add(player.Connection.Id, player);
  }

  public void DetachPlayer(RagonRoomPlayer roomPlayer)
  {
    if (Players.Remove(roomPlayer.Connection.Id, out var player))
    {
      PlayerList.Remove(player);

      {
        Writer.Clear();
        Writer.WriteOperation(RagonOperation.PLAYER_LEAVED);
        Writer.WriteString(player.Id);

        var entitiesToDelete = player.Entities.DynamicList;
        Writer.WriteUShort((ushort)entitiesToDelete.Count);
        foreach (var entity in entitiesToDelete)
        {
          Writer.WriteUShort(entity.Id);
          DetachEntity(entity);
        }

        var sendData = Writer.ToArray();
        Broadcast(sendData);
      }

      if (roomPlayer.Connection.Id == Owner.Connection.Id && PlayerList.Count > 0)
      {
        var nextOwner = PlayerList[0];

        Owner = nextOwner;

        var entitiesToUpdate = roomPlayer.Entities.StaticList;

        Writer.Clear();
        Writer.WriteOperation(RagonOperation.OWNERSHIP_ENTITY_CHANGED);
        Writer.WriteUShort(Owner.Connection.Id);
        Writer.WriteUShort((ushort)entitiesToUpdate.Count);

        foreach (var entity in entitiesToUpdate)
        {
          Writer.WriteUShort(entity.Id);

          entity.Attach(nextOwner);
          nextOwner.Entities.Add(entity);
        }

        var sendData = Writer.ToArray();
        Broadcast(sendData);
      }

      player.OnDetached();

      UpdateReadyPlayerList();
      
      Plugin.OnPlayerLeaved(player);
    }
  }

  public void UpdateReadyPlayerList()
  {
    ReadyPlayersList = PlayerList.Where(p => p.IsLoaded).ToList();
  }

  public void UpdateMap(string sceneName)
  {
    Scene = sceneName;

    DynamicEntitiesList.Clear();
    StaticEntitiesList.Clear();
    Entities.Clear();
    EntityList.Clear();

    foreach (var player in PlayerList)
      player.UnsetReady();

    UpdateReadyPlayerList();
  }

  public void Track(RagonEntity entity)
  {
    _entitiesDirtySet.Add(entity);
  }

  public void Broadcast(byte[] data, NetworkChannel channel = NetworkChannel.RELIABLE)
  {
    if (channel == NetworkChannel.RELIABLE)
    {
      foreach (var readyPlayer in ReadyPlayersList)
        readyPlayer.Connection.Reliable.Send(data);
    }
    else
    {
      foreach (var readyPlayer in ReadyPlayersList)
        readyPlayer.Connection.Unreliable.Send(data);
    }
  }

  public void Broadcast(byte[] data, List<RagonRoomPlayer> players, NetworkChannel channel = NetworkChannel.RELIABLE)
  {
    if (channel == NetworkChannel.RELIABLE)
    {
      foreach (var p in players)
        p.Connection.Reliable.Send(data);
    }
    else
    {
      foreach (var p in players)
        p.Connection.Unreliable.Send(data);
    }
  }

  public RagonRoomPlayer GetPlayerByConnection(INetworkConnection connection)
  {
    return Players[connection.Id];
  }

  public RagonRoomPlayer? GetPlayerById(string id)
  {
    return PlayerList.FirstOrDefault(p => p.Id == id);
  }

  public IRagonEntity? GetEntityById(ushort id)
  {
    return Entities.TryGetValue(id, out var entity) ? entity : null;
  }

  public IRagonEntity[] GetEntitiesOfPlayer(RagonRoomPlayer player)
  {
    return EntityList.Where(e => e.Owner.Connection.Id == player.Connection.Id).ToArray();
  }

  public void Attach()
  {
    Plugin.OnAttached(this);
  }
  
  public void Detach()
  {
    Plugin.OnDetached(this);
    
    Players.Clear();
    WaitPlayersList.Clear();
    ReadyPlayersList.Clear();
    PlayerList.Clear();
    
    Entities.Clear();
    DynamicEntitiesList.Clear();
    StaticEntitiesList.Clear();
    EntityList.Clear();
    
    _entitiesDirtySet.Clear();
    _bufferedEvents.Clear();
    
    IsDone = true;
  }
}