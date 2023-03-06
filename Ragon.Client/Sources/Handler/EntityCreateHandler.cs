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

internal  class EntityCreateHandler : Handler
{
  private readonly RagonClient _client;
  private readonly RagonPlayerCache _playerCache;
  private readonly RagonEntityCache _entityCache;
  
  public EntityCreateHandler(
    RagonClient client,
    RagonPlayerCache playerCache,
    RagonEntityCache entityCache
  )
  {
    _client = client;
    _entityCache = entityCache;
    _playerCache = playerCache;
  }

  public void Handle(RagonBuffer buffer)
  {
    var attachId = buffer.ReadUShort();
    var entityType = buffer.ReadUShort();
    var entityId = buffer.ReadUShort();
    var ownerId = buffer.ReadUShort();
    var player = _playerCache.GetPlayerByPeer(ownerId);
    var payload = new RagonPayload(buffer.Capacity);
    payload.Read(buffer);

    if (player == null)
    {
      RagonLog.Warn($"Owner {ownerId}|{player.Name} not found in players");
      return;
    }
    
    var hasAuthority = _playerCache.LocalPlayer.Id == player.Id;
    var entity = _entityCache.OnCreate(attachId, entityType, 0, entityId, hasAuthority);
    entity.Attach(_client, entityId, entityType, hasAuthority, player, payload);
  }
}