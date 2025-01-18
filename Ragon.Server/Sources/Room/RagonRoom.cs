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
using Ragon.Server.Event;
using Ragon.Server.IO;
using Ragon.Server.Plugin;
using Ragon.Server.Time;

namespace Ragon.Server.Room;

public class RagonRoom : IRagonRoom, IRagonAction
{
  public string Id { get; private set; }
  public int PlayerMax { get; private set; }
  public int PlayerMin { get; private set; }
  public int PlayerCount => WaitPlayersList.Count;
  public IReadOnlyList<RagonRoomPlayer> GetPlayers() => PlayerList;
  
  public bool IsDone { get; private set; }

  public RagonData UserData { get; set; }
  public RagonRoomPlayer Owner { get; private set; }
  public RagonStream Writer { get; }
  public IRoomPlugin Plugin { get; private set; }

  public Dictionary<ushort, RagonRoomPlayer> Players { get; private set; }
  public List<RagonRoomPlayer> WaitPlayersList { get; private set; }
  public List<RagonRoomPlayer> ReadyPlayersList { get; private set; }
  public List<RagonRoomPlayer> PlayerList { get; private set; }

  private readonly List<RagonEvent> _bufferedEvents;
  private readonly int _limitBufferedEvents;

  public RagonRoom(string roomId, RoomInformation info, IRoomPlugin roomPlugin)
  {
    Id = roomId;
    PlayerMax = info.Max;
    PlayerMin = info.Min;
    Plugin = roomPlugin;

    Players = new Dictionary<ushort, RagonRoomPlayer>(info.Max);
    WaitPlayersList = new List<RagonRoomPlayer>(info.Max);
    ReadyPlayersList = new List<RagonRoomPlayer>(info.Max);
    PlayerList = new List<RagonRoomPlayer>(info.Max);

    _bufferedEvents = new List<RagonEvent>();
    _limitBufferedEvents = 1000;

    UserData = new RagonData();
    Writer = new RagonStream();
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
  
  public void Tick(float dt)
  {
   
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
    
    _bufferedEvents.Clear();
    
    IsDone = true;
  }
}