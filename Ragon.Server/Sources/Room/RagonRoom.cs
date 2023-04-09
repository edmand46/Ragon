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
using Ragon.Server.Plugin;
using Ragon.Server.Time;

namespace Ragon.Server.Room;

public class RagonRoom : IRagonAction
{
  public string Id { get; private set; }
  public string Map { get; private set; }
  public int PlayerMax { get; private set; }
  public int PlayerMin { get; private set; }
  public int PlayerCount => WaitPlayersList.Count;
  
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

  public RagonRoom(string roomId, RoomInformation info, IRoomPlugin roomPlugin)
  {
    Id = roomId;
    Map = info.Map;
    PlayerMax = info.Max;
    PlayerMin = info.Min;
    Plugin = roomPlugin;

    Players = new Dictionary<ushort, RagonRoomPlayer>(info.Max);
    WaitPlayersList = new List<RagonRoomPlayer>(info.Max);
    ReadyPlayersList = new List<RagonRoomPlayer>(info.Max);
    PlayerList = new List<RagonRoomPlayer>(info.Max);

    Entities = new Dictionary<ushort, RagonEntity>();
    DynamicEntitiesList = new List<RagonEntity>();
    StaticEntitiesList = new List<RagonEntity>();
    EntityList = new List<RagonEntity>();

    _entitiesDirtySet = new HashSet<RagonEntity>();

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

  public void Tick(float dt)
  {
    var entities = (ushort)_entitiesDirtySet.Count;
    if (entities > 0)
    {
      Writer.Clear();
      Writer.WriteOperation(RagonOperation.REPLICATE_ENTITY_STATE);
      Writer.WriteUShort(entities);

      foreach (var entity in _entitiesDirtySet)
        entity.Write(Writer);

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
        Writer.WriteOperation(RagonOperation.OWNERSHIP_CHANGED);
        Writer.WriteString(Owner.Id);
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
    }
  }

  public void UpdateReadyPlayerList()
  {
    ReadyPlayersList = PlayerList.Where(p => p.IsLoaded).ToList();
  }

  public void UpdateMap(string sceneName)
  {
    Map = sceneName;

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

  public void Broadcast(byte[] data)
  {
    foreach (var readyPlayer in ReadyPlayersList)
      readyPlayer.Connection.Reliable.Send(data);
  }
}