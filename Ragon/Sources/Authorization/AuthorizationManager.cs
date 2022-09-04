using System;
using System.Collections.Generic;
using NLog;
using Ragon.Common;

namespace Ragon.Core;

public class AuthorizationManager : IAuthorizationManager
{
  private Logger _logger = LogManager.GetCurrentClassLogger();
  private IAuthorizationProvider _provider;
  private IGameThread _gameThread;
  private Lobby _lobby;
  private RagonSerializer _serializer;
  private readonly Dictionary<uint, Player> _playersByPeers;
  private readonly Dictionary<string, Player> _playersByIds;

  public AuthorizationManager(IAuthorizationProvider provider, IGameThread gameThread, Lobby lobby, RagonSerializer serializer)
  {
    _serializer = serializer;
    _lobby = lobby;
    _provider = provider;
    _gameThread = gameThread;
    _playersByIds = new Dictionary<string, Player>();
    _playersByPeers = new Dictionary<uint, Player>();
  }

  public void OnAuthorization(uint peerId, string key, string name)
  {
    if (_playersByPeers.ContainsKey(peerId))
    {
     _logger.Warn($"Connection already authorized {peerId}"); 
      return;
    }
    
    var dispatcher = _gameThread.ThreadDispatcher;
    
    _provider.OnAuthorizationRequest(key, name, Array.Empty<byte>(),
      (playerId, playerName) => { dispatcher.Dispatch(() => Accepted(peerId, playerId, playerName)); },
      (errorCode) => { dispatcher.Dispatch(() => Rejected(peerId, errorCode)); });
  }

  public void Accepted(uint peerId, string playerId, string playerName)
  {
    _serializer.Clear();
    _serializer.WriteOperation(RagonOperation.AUTHORIZED_SUCCESS);
    _serializer.WriteString(playerId);
    _serializer.WriteString(playerName);

    var player = new Player()
    {
      Id = playerId,
      PlayerName = playerName,
      PeerId = peerId,
      IsLoaded = false,
      Entities = new List<Entity>(),
      EntitiesIds = new List<ushort>(),
    };

    _playersByIds.Add(playerId, player);
    _playersByPeers.Add(peerId, player);

    var sendData = _serializer.ToArray();
    _gameThread.Server.Send(peerId, sendData, DeliveryType.Reliable);
  }

  public void Rejected(uint peerId, uint code)
  {
    _serializer.Clear();
    _serializer.WriteOperation(RagonOperation.AUTHORIZED_FAILED);
    _serializer.WriteInt((int) code);

    var sendData = _serializer.ToArray();
    _gameThread.Server.Send(peerId, sendData, DeliveryType.Reliable);
    _gameThread.Server.Disconnect(peerId, 0);
  }

  public void Cleanup(uint peerId)
  {
    if (_playersByPeers.Remove(peerId, out var player))
      _playersByIds.Remove(player.Id);
  }

  public Player? GetPlayer(uint peerId)
  {
    if (_playersByPeers.TryGetValue(peerId, out var player))
      return player;

    return null;
  }

  public Player GetPlayer(string playerId)
  {
    return _playersByIds[playerId];
  }
}