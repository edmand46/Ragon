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

namespace Ragon.Client;

public struct RagonRoomInformation
{
  public RagonRoomInformation(string roomId, string playerId, string ownerId, ushort min, ushort max)
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

internal class JoinSuccessHandler : Handler
{
  private readonly RagonListenerList _listenerList;
  private readonly RagonPlayerCache _playerCache;
  private readonly RagonEntityCache _entityCache;
  private readonly RagonClient _client;
  private readonly RagonBuffer _buffer;

  public JoinSuccessHandler(
    RagonClient client,
    RagonBuffer buffer,
    RagonListenerList listenerList,
    RagonPlayerCache playerCache,
    RagonEntityCache entityCache
  )
  {
    _buffer = buffer;
    _client = client;
    _listenerList = listenerList;
    _entityCache = entityCache;
    _playerCache = playerCache;
  }

  public void Handle(RagonBuffer buffer)
  {
    var roomId = buffer.ReadString();
    var localId = buffer.ReadString();
    var ownerId = buffer.ReadString();
    var min = buffer.ReadUShort();
    var max = buffer.ReadUShort();
    var sceneName = buffer.ReadString();

    var scene = new RagonScene(_client, _playerCache, _entityCache, sceneName);
    var roomInfo = new RagonRoomInformation(roomId, localId, ownerId, min, max);
    var room = new RagonRoom(_client, _entityCache, _playerCache, roomInfo, scene);

    _playerCache.SetOwnerAndLocal(ownerId, localId);
    _client.AssignRoom(room);
    
    _listenerList.OnSceneRequest(sceneName);
  }
}