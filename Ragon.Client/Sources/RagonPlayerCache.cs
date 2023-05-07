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

namespace Ragon.Client;

public sealed class RagonPlayerCache
{
  private List<RagonPlayer> _players = new List<RagonPlayer>();
  private Dictionary<string, RagonPlayer> _playersById = new();
  private Dictionary<ushort, RagonPlayer> _playersByConnection = new();

  public RagonPlayer Owner { get; private set; }
  public RagonPlayer LocalPlayer { get; private set; }
  public bool IsRoomOwner => _ownerId == _localId;
  
  public RagonPlayer? GetPlayerById(string playerId) => _playersById[playerId];
  public RagonPlayer? GetPlayerByPeer(ushort peerId) => _playersByConnection[peerId];

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
      LocalPlayer = player;

    if (player.IsRoomOwner)
      Owner = player;

    _players.Add(player);
    _playersById.Add(player.Id, player);
    _playersByConnection.Add(player.PeerId, player);
  }

  public void RemovePlayer(string playerId)
  {
    if (_playersById.Remove(playerId, out var player))
    {
      _players.Remove(player);
      _playersByConnection.Remove(player.PeerId);
    }
  }

  public void OnOwnershipChanged(string playerId)
  {
    foreach (var player in _players)
    {
      if (player.Id == playerId)
        Owner = player;
      player.IsRoomOwner = player.Id == playerId;
    }
  }


  public void Cleanup()
  {
    _players.Clear();
    _playersByConnection.Clear();
    _playersById.Clear();
  }
}