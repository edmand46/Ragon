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

namespace Ragon.Client;

public struct RoomParameters
{
  public RoomParameters(string roomId, string playerId, string ownerId, ushort min, ushort max)
  {
    RoomId = roomId;
    PlayerId = playerId;
    OwnerId = ownerId;
    Min = min;
    Max = max;
  }

  public string RoomId { get; private set; }
  public string PlayerId { get; private set; }
  public string OwnerId { get; private set; }
  public ushort Min { get; private set; }
  public ushort Max { get; private set; }
}

internal class JoinSuccessHandler : IHandler
{
  private readonly RagonListenerList _listenerList;
  private readonly RagonPlayerCache _playerCache;
  private readonly RagonEntityCache _entityCache;
  private readonly RagonClient _client;

  public JoinSuccessHandler(
    RagonClient client,
    RagonListenerList listenerList,
    RagonPlayerCache playerCache,
    RagonEntityCache entityCache
  )
  {
    _client = client;
    _listenerList = listenerList;
    _entityCache = entityCache;
    _playerCache = playerCache;
  }

  public void Handle(RagonBuffer reader)
  {
    var roomId = reader.ReadString();
    var min = reader.ReadUShort();
    var max = reader.ReadUShort();
    var sceneName = reader.ReadString();
    var localId = reader.ReadString();
    var ownerId = reader.ReadString();
    
    _playerCache.SetOwnerAndLocal(ownerId, localId);
    
    var scene = new RagonScene(_client, _playerCache, _entityCache, sceneName);
    var roomInfo = new RoomParameters(roomId, localId, ownerId, min, max);
    var room = new RagonRoom(_client, _entityCache, _playerCache, roomInfo, scene);

    room.UserData.Read(reader);
      
    var playersCount = reader.ReadUShort();
    RagonLog.Trace("Players: " + playersCount);
    for (var i = 0; i < playersCount; i++)
    {
      var playerPeerId = reader.ReadUShort();
      var playerId = reader.ReadString();
      var playerName = reader.ReadString();

      var player = _playerCache.AddPlayer(playerPeerId, playerId, playerName);
      
      player.UserData.Read(reader);

      RagonLog.Trace($"Player {playerPeerId} - {playerId} - {playerName}");
    }
    
    _client.AssignRoom(room);
    _client.SetStatus(RagonStatus.ROOM);
    
    _listenerList.OnJoined();
    _listenerList.OnSceneRequest(sceneName);
  }
}