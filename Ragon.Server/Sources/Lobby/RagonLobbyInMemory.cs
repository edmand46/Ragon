/*
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

using System.Diagnostics.CodeAnalysis;
using Ragon.Server.Logging;
using Ragon.Server.Room;

namespace Ragon.Server.Lobby;

public class LobbyInMemory : IRagonLobby
{
  private readonly List<RagonRoom> _rooms = new();
  private readonly IRagonLogger _logger = LoggerManager.GetLogger(nameof(LobbyInMemory));

  public IReadOnlyList<IRagonRoom> Rooms => _rooms.AsReadOnly();

  public bool FindRoomById(string roomId, [MaybeNullWhen(false)] out RagonRoom room)
  {
    foreach (var existRagonRoom in _rooms)
    {
      if (existRagonRoom.Id == roomId)
      {
        if (existRagonRoom.PlayerCount >= existRagonRoom.PlayerMax)
        {
          _logger.Warning($"Room with id {roomId} fulfilled");
          
          room = default;
          return false;
        }
        
        room = existRagonRoom;
        return true;
      }
    }

    room = default;
    return false;
  }

  public bool FindRoomByScene(string sceneName, [MaybeNullWhen(false)] out RagonRoom room)
  {
    foreach (var existsRoom in _rooms)
    {
      if (existsRoom.Scene == sceneName)
      {
        if (existsRoom.PlayerCount >= existsRoom.PlayerMax)
        {
          _logger.Warning($"Room with scene {sceneName} fulfilled");
          
          room = default;
          return false;          
        }
        
        room = existsRoom;
        return true;
      }
    }

    room = default;
    return false;
  }

  public void Persist(RagonRoom room)
  {
    room.Attach();
    
    _rooms.Add(room);
    _logger.Trace($"New room: {room.Id}");
    
    foreach (var r in _rooms)
      _logger.Trace($"Room: {r.Id} Scene: {r.Scene} Players: {r.Players.Count}");
  }

  public bool RemoveIfEmpty(RagonRoom room)
  {
    var result = false;
    if (room.Players.Count == 0)
    {
      room.Detach();
      
      _rooms.Remove(room);
      _logger.Trace($"Room {room.Id} removed");

      result = true;
    }

    foreach (var r in _rooms)
      _logger.Trace($"Room: {r.Id} Scene: {r.Scene} Players: {r.Players.Count}");

    return result;
  }
}