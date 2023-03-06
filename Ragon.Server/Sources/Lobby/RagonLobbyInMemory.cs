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

namespace Ragon.Server;

public class LobbyInMemory : IRagonLobby
{
  private readonly List<RagonRoom> _rooms = new();
  private readonly Logger _logger = LogManager.GetCurrentClassLogger();

  public bool FindRoomById(string RagonRoomId, [MaybeNullWhen(false)] out RagonRoom room)
  {
    foreach (var existRagonRoom in _rooms)
    {
      var info = existRagonRoom.Info;
      if (existRagonRoom.Id == RagonRoomId && info.Min < info.Max)
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
      var info = existsRoom.Info;
      if (info.Map == map && existsRoom.Players.Count < info.Max)
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
      _logger.Trace($"Room: {r.Id} {r.Info} Players: {r.Players.Count} Entities: {r.Entities.Count}");
  }

  public void RemoveIfEmpty(RagonRoom room)
  {
    if (room.Players.Count == 0)
    {
      _rooms.Remove(room);
      _logger.Trace($"Room {room.Id} removed");
    }

    foreach (var r in _rooms)
      _logger.Trace($"Room: {r.Id} {r.Info} Players: {r.Players.Count} Entities: {r.Entities.Count}");
  }
}