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

namespace Ragon.Client;

public sealed class RagonPlayerCache
{
  private readonly List<RagonPlayer> _players = new();
  private readonly Dictionary<string, RagonPlayer> _playersById = new();
  private readonly Dictionary<ushort, RagonPlayer> _playersByConnection = new();

  public IReadOnlyList<RagonPlayer> Players => _players;
  public RagonPlayer Owner { get; private set; }
  public RagonPlayer Local { get; private set; }
  public bool IsRoomOwner => _ownerId == _localId;

  public RagonPlayer? GetPlayerById(string playerId)
  {
    if (_playersById.TryGetValue(playerId, out var player))
      return player;

    return null;
  }

  public RagonPlayer? GetPlayerByPeer(ushort peerId)
  {
    if (_playersByConnection.TryGetValue(peerId, out var player))
      return player;

    return null;
  }

  private string _ownerId;
  private string _localId;

  public void SetOwnerAndLocal(string ownerId, string localId)
  {
    _ownerId = ownerId;
    _localId = localId;
  }

  public void AddPlayer(ushort peerId, string playerId, string playerName)
  {
    if (_playersById.ContainsKey(playerId))
      return;

    var isOwner = playerId == _ownerId;
    var isLocal = playerId == _localId;

    RagonLog.Trace($"Added player {peerId}|{playerId}|{playerName} IsOwner: {isOwner} isLocal: {isLocal}");

    var player = new RagonPlayer(peerId, playerId, playerName, isOwner, isLocal);

    if (player.IsLocal)
      Local = player;

    if (player.IsRoomOwner)
      Owner = player;

    _players.Add(player);
    _playersById.Add(player.Id, player);
    _playersByConnection.Add(player.PeerId, player);
  }

  public void RemovePlayer(string playerId)
  {
    if (_playersById.TryGetValue(playerId, out var player))
    {
      _players.Remove(player);
      _playersById.Remove(playerId);
      _playersByConnection.Remove(player.PeerId);
    }
  }

  public void OnOwnershipChanged(ushort playerPeerId)
  {
    foreach (var player in _players)
    {
      if (player.PeerId == playerPeerId)
      {
        Owner = player;
        Owner.IsRoomOwner = true;
      }
    }
  }


  public void Cleanup()
  {
    _players.Clear();
    _playersByConnection.Clear();
    _playersById.Clear();
  }

  public void Dump()
  {
    RagonLog.Trace("Players: ");
    RagonLog.Trace("[Connection] [ID] [Name]");
    foreach (var player in _players)
    {
      RagonLog.Trace($"[{player.PeerId}] {player.Id} {player.Name}");
    }
  }
}