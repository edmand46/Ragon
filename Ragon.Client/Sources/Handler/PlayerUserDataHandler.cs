/*
 * Copyright 2024 Eduard Kargin <kargin.eduard@gmail.com>
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

namespace Ragon.Client
{

  internal class PlayerUserDataHandler: IHandler
  {
    private RagonPlayerCache _playerCache;
    private RagonListenerList _listenerList;

    public PlayerUserDataHandler(
      RagonPlayerCache playerCache,
      RagonListenerList listenerList
    )
    {
      _playerCache = playerCache;
      _listenerList = listenerList;
    }
    public void Handle(RagonBuffer reader)
    {
      var playerPeerId = reader.ReadUShort();
      var player = _playerCache.GetPlayerByPeer(playerPeerId);

      if (player != null)
      {
        var changes = player.UserData.Read(reader);
        
        _listenerList.OnPlayerUserData(player, changes);
        
        return;
      }
      
      RagonLog.Warn("Received user data for unknown player.");
    }
  }
}