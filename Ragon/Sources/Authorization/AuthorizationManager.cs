using System;
using System.Collections.Generic;
using Ragon.Common;

namespace Ragon.Core;

public class AuthorizationManager : IAuthorizationManager
{
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

  public void OnAuthorization(uint peerId, string key, string name, byte protocol)
  {
    var dispatcher = _gameThread.GetDispatcher();
    _provider.OnAuthorizationRequest(key, name, protocol, Array.Empty<byte>(),
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
      EntitiesIds = new List<int>(),
    };
    
    _playersByIds.Add(playerId, player);
    _playersByPeers.Add(peerId, player);
    
    var sendData = _serializer.ToArray();
    _gameThread.SendSocketEvent(new SocketEvent() {Data = sendData, PeerId = peerId, Type = EventType.DATA, Delivery = DeliveryType.Reliable});
  }

  public void Rejected(uint peerId, uint code)
  {
    _serializer.Clear();
    _serializer.WriteOperation(RagonOperation.AUTHORIZED_FAILED);
    _serializer.WriteInt((int) code);

    var sendData = _serializer.ToArray();
    _gameThread.SendSocketEvent(new SocketEvent() {Data = sendData, PeerId = peerId, Type = EventType.DATA, Delivery = DeliveryType.Reliable});
    var emtpyData = Array.Empty<byte>();
    _gameThread.SendSocketEvent(new SocketEvent() {Data = emtpyData, PeerId = peerId, Type = EventType.DISCONNECTED, Delivery = DeliveryType.Reliable});
  }

  public void Cleanup(uint peerId)
  {
    if (_playersByPeers.Remove(peerId, out var player))
      _playersByIds.Remove(player.Id);
  }

  public Player GetPlayer(uint peerId)
  {
    return _playersByPeers[peerId];
  }
  
  public Player GetPlayer(string playerId)
  {
    return _playersByIds[playerId];
  }
}