﻿/*
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

public class RagonScene
{
  public string Name { get; private set; }
  
  private readonly RagonClient _client;
  private readonly RagonEntityCache _entityCache;
  private readonly RagonPlayerCache _playerCache;
  
  public RagonScene(RagonClient client, RagonPlayerCache playerCache, RagonEntityCache entityCache, string sceneName)
  {
    Name = sceneName;
    
    _client = client;
    _playerCache = playerCache;
    _entityCache = entityCache;
  }

  internal void Update(string scene)
  {
    Name = scene;
  }
  
  internal void Load(string sceneName)
  {
    var buffer = _client.Buffer;
    
    buffer.Clear();
    buffer.WriteOperation(RagonOperation.LOAD_SCENE);
    buffer.WriteString(sceneName);

    var sendData = buffer.ToArray();
    _client.Reliable.Send(sendData);
  }

  internal void SceneLoaded()
  {
    var buffer = _client.Buffer;
    
    buffer.Clear();
    buffer.WriteOperation(RagonOperation.SCENE_LOADED);

    if (_playerCache.IsRoomOwner)
      _entityCache.WriteScene(buffer);
    else
      _entityCache.CacheScene();
    
    var sendData = buffer.ToArray();
    _client.Reliable.Send(sendData);
  }

  internal void ReplicateEvent<TEvent>(TEvent evnt, RagonTarget target, RagonReplicationMode replicationMode)
    where TEvent : IRagonEvent, new()
  {
    var evntId = _client.Event.GetEventCode(evnt);
    var buffer = _client.Buffer;
    
    buffer.Clear();
    buffer.WriteOperation(RagonOperation.REPLICATE_ROOM_EVENT);
    buffer.WriteUShort(evntId);
    buffer.WriteByte((byte)replicationMode);
    buffer.WriteByte((byte)target);
    
    evnt.Serialize(buffer);
    
    var sendData = buffer.ToArray();
    _client.Reliable.Send(sendData);
  }
  
  internal void ReplicateEvent<TEvent>(TEvent evnt, RagonPlayer target, RagonReplicationMode replicationMode)
    where TEvent : IRagonEvent, new()
  {
    var evntId = _client.Event.GetEventCode(evnt);
    var buffer = _client.Buffer;

    buffer.Clear();
    buffer.WriteOperation(RagonOperation.REPLICATE_ROOM_EVENT);
    buffer.WriteUShort(evntId);
    buffer.WriteByte((byte)replicationMode);
    buffer.WriteByte((byte)RagonTarget.Player);
    buffer.WriteUShort(target.PeerId);

    evnt.Serialize(buffer);

    var sendData = buffer.ToArray();
    _client.Reliable.Send(sendData);
  }

  public void ReplicateData(byte[] data, bool reliable)
  {
    var sendData = new byte[data.Length + 1];
    sendData[0] = (byte) RagonOperation.REPLICATE_RAW_DATA;
    Array.Copy(data, 0, sendData, 1, data.Length);
    
    if (reliable)
      _client.Reliable.Send(sendData);
    else
      _client.Unreliable.Send(sendData);
  }
}