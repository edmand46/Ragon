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

public class RoomEventHandler: IHandler
{
  private readonly RagonClient _client;
  private readonly RagonPlayerCache _playerCache;
  
  public RoomEventHandler(
    RagonClient client,
    RagonPlayerCache playerCache
  )
  {
    _client = client;
    _playerCache = playerCache;
  }
  
  public void Handle(RagonBuffer buffer)
  {
    var eventCode = buffer.ReadUShort();
    var peerId = buffer.ReadUShort();
    var executionMode = (RagonReplicationMode)buffer.ReadByte();
    
    var player = _playerCache.GetPlayerByPeer(peerId);
    if (player == null)
    {
      RagonLog.Warn($"Player not found for event {eventCode}");
      return;
    }

    if (player.IsLocal && executionMode == RagonReplicationMode.LocalAndServer)
      return;

    _client.Room.Event(eventCode, player, buffer); 
  }
}