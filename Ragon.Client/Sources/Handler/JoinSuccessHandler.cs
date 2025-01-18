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


using Ragon.Protocol;

namespace Ragon.Client;

public struct RoomParameters
{
  public RoomParameters(string roomId, string playerId, string ownerId, ushort min, ushort max)
  {
    RoomId = roomId;
    PlayerId = playerId;
    OwnerId = ownerId;
    Min = min;
    Max = max;
  }

  public string RoomId { get; private set; }
  public string PlayerId { get; private set; }
  public string OwnerId { get; private set; }
  public ushort Min { get; private set; }
  public ushort Max { get; private set; }
}

internal class JoinSuccessHandler : IHandler
{
  private readonly RagonListenerList _listenerList;
  private readonly RagonPlayerCache _playerCache;
  private readonly RagonClient _client;
  private readonly RagonRoom _room;

  public JoinSuccessHandler(
    RagonClient client,
    RagonRoom room
  )
  {
    _client = client;
    _room = room;
  }

  public void Handle(RagonStream reader)
  {
    var roomId = reader.ReadString();
    var min = reader.ReadUShort();
    var max = reader.ReadUShort();
    var localId = reader.ReadString();
    var ownerId = reader.ReadString();
    var roomInfo = new RoomParameters(roomId, localId, ownerId, min, max);
    
    _playerCache.SetOwnerAndLocal(ownerId, localId);
    _room.Reset(roomInfo);
    _room.UserData.Read(reader);

    var playersCount = reader.ReadUShort();
    RagonLog.Trace("Players: " + playersCount);
    for (var i = 0; i < playersCount; i++)
    {
      var playerPeerId = reader.ReadUShort();
      var playerId = reader.ReadString();
      var playerName = reader.ReadString();

      var player = _playerCache.AddPlayer(playerPeerId, playerId, playerName);

      player.UserData.Read(reader);

      RagonLog.Trace($"Player {playerPeerId} - {playerId} - {playerName}");
    }

    _client.UpdateState(RagonState.ROOM);

    _listenerList.OnJoined();
  }
}