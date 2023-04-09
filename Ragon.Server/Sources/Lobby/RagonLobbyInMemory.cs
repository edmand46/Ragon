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

using System.Diagnostics.CodeAnalysis;
using NLog;
using Ragon.Server.Room;

namespace Ragon.Server.Lobby;

public class LobbyInMemory : IRagonLobby
{
  private readonly List<RagonRoom> _rooms = new();
  private readonly Logger _logger = LogManager.GetCurrentClassLogger();

  public bool FindRoomById(string RagonRoomId, [MaybeNullWhen(false)] out RagonRoom room)
  {
    foreach (var existRagonRoom in _rooms)
    {
      if (existRagonRoom.Id == RagonRoomId && existRagonRoom.PlayerMin < existRagonRoom.PlayerMax)
      {
        room = existRagonRoom;
        return true;
      }
    }

    room = default;
    return false;
  }

  public bool FindRoomByMap(string map, [MaybeNullWhen(false)] out RagonRoom room)
  {
    foreach (var existsRoom in _rooms)
    {
      if (existsRoom.Map == map && existsRoom.PlayerCount < existsRoom.PlayerMax)
      {
        room = existsRoom;
        return true;
      }
    }

    room = default;
    return false;
  }

  public void Persist(RagonRoom room)
  {
    _rooms.Add(room);
    _logger.Trace($"New room: {room.Id}");
    
    foreach (var r in _rooms)
      _logger.Trace($"Room: {r.Id} Map: {r.Map} Players: {r.Players.Count} Entities: {r.Entities.Count}");
  }

  public bool RemoveIfEmpty(RagonRoom room)
  {
    var result = false;
    if (room.Players.Count == 0)
    {
      _rooms.Remove(room);
      _logger.Trace($"Room {room.Id} removed");

      result = true;
    }

    foreach (var r in _rooms)
      _logger.Trace($"Room: {r.Id} Map: {r.Map} Players: {r.Players.Count} Entities: {r.Entities.Count}");

    return result;
  }
}