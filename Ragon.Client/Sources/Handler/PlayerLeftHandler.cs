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

internal class PlayerLeftHandler : IHandler
{
  private RagonPlayerCache _playerCache;
  private RagonEntityCache _entityCache;
  private RagonListenerList _listenerList;

  public PlayerLeftHandler(
    RagonEntityCache entityCache,
    RagonPlayerCache playerCache,
    RagonListenerList listenerList
  )
  {
    _entityCache = entityCache;
    _playerCache = playerCache;
    _listenerList = listenerList;
  }

  public void Handle(RagonBuffer reader)
  {
    var playerId = reader.ReadString();
    var player = _playerCache.GetPlayerById(playerId);
    if (player != null)
    {
      _playerCache.RemovePlayer(playerId);
      _listenerList.OnPlayerLeft(player);

      var entities = reader.ReadUShort();
      var toDeleteIds = new ushort[entities];
      for (var i = 0; i < entities; i++)
      {
        var entityId = reader.ReadUShort();
        toDeleteIds[i] = entityId;
      }

      var emptyPayload = new RagonPayload(0);
      foreach (var id in toDeleteIds)
        _entityCache.OnDestroy(id, emptyPayload);
    }
    else
    {
      RagonLog.Warn($"Player with Id:{playerId} not found in cache");
    }
  }
}