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

internal class PlayerJoinHandler : Handler
{
  private RagonPlayerCache _playerCache;
  private RagonListenerList _listenerList;

  public PlayerJoinHandler(
    RagonPlayerCache playerCache,
    RagonListenerList listenerList
  )
  {
    _playerCache = playerCache;
    _listenerList = listenerList;
  }

  public void Handle(RagonBuffer buffer)
  {
    var playerPeerId = buffer.ReadUShort();
    var playerId = buffer.ReadString();
    var playerName = buffer.ReadString();

    _playerCache.AddPlayer(playerPeerId, playerId, playerName);

    var player = _playerCache.GetPlayerById(playerId);
    if (player != null)
      _listenerList.OnPlayerJoined(player);
    else
      RagonLog.Trace($"[Joined] {playerId}");
  }
}