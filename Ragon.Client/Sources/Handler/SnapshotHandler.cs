﻿/*
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

internal class SnapshotHandler : Handler
{
  private RagonClient _client;
  private RagonListenerList _listenerList;
  private RagonEntityCache _entityCache;
  private RagonPlayerCache _playerCache;

  public SnapshotHandler(
    RagonClient ragonClient,
    RagonListenerList listenerList,
    RagonEntityCache entityCache,
    RagonPlayerCache playerCache
  )
  {
    _client = ragonClient;
    _listenerList = listenerList;
    _entityCache = entityCache;
    _playerCache = playerCache;
  }

  public void Handle(RagonBuffer buffer)
  {
    var playersCount = buffer.ReadUShort();
    RagonLog.Trace("Players: " + playersCount);
    for (var i = 0; i < playersCount; i++)
    {
      var playerPeerId = buffer.ReadUShort();
      var playerId = buffer.ReadString();
      var playerName = buffer.ReadString();

      _playerCache.AddPlayer(playerPeerId, playerId, playerName);
    }
    var dynamicEntities = buffer.ReadUShort();
    RagonLog.Trace("Dynamic Entities: " + dynamicEntities);
    for (var i = 0; i < dynamicEntities; i++)
    {
      var entityType = buffer.ReadUShort();
      var entityId = buffer.ReadUShort();
      var ownerPeerId = buffer.ReadUShort();
      var payloadSize = buffer.ReadUShort();
      var player = _playerCache.GetPlayerByPeer(ownerPeerId);
      
      var payload = new RagonPayload(payloadSize);
      payload.Read(buffer);

      var hasAuthority = _playerCache.LocalPlayer.Id == player.Id;
      var entity = _entityCache.OnCreate(0, entityType, 0, entityId, hasAuthority);
      
      entity.Read(buffer);
      entity.Attach(_client, entityId, entityType, hasAuthority, player, payload);
    }

    var staticEntities = buffer.ReadUShort();
    RagonLog.Trace("Scene Entities: " + staticEntities);
    for (var i = 0; i < staticEntities; i++)
    {
      var entityType = buffer.ReadUShort();
      var entityId = buffer.ReadUShort();
      var staticId = buffer.ReadUShort();
      var ownerPeerId = buffer.ReadUShort();
      var payloadSize = buffer.ReadUShort();
      var player = _playerCache.GetPlayerByPeer(ownerPeerId);
      
      var payload = new RagonPayload(payloadSize);
      payload.Read(buffer);

      var hasAuthority = _playerCache.LocalPlayer.Id == player.Id;
      var entity = _entityCache.OnCreate(0, entityType, staticId, entityId, hasAuthority);
      
      entity.Read(buffer);
      entity.Attach(_client, entityId, entityType, hasAuthority, player, payload);
    }
    
    _listenerList.OnJoined();
  }
}