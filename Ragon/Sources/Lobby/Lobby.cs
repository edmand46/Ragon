using System;
using System.Linq;
using NLog;
using Ragon.Common;

namespace Ragon.Core;

public class Lobby
{
  private readonly ILogger _logger = LogManager.GetCurrentClassLogger();
  private readonly RagonSerializer _serializer;
  private readonly RoomManager _roomManager;
  private readonly AuthorizationManager _authorizationManager;
  private readonly Application _gameThread;

  public AuthorizationManager AuthorizationManager => _authorizationManager;

  public Lobby(IAuthorizationProvider provider, RoomManager manager, Application gameThread)
  {
    _roomManager = manager;
    _gameThread = gameThread;
    _serializer = new RagonSerializer();
    _authorizationManager = new AuthorizationManager(provider, gameThread, this, _serializer);
  }

  public void ProcessEvent(ushort peerId, RagonOperation op, ReadOnlySpan<byte> payload)
  {
    _serializer.Clear();
    _serializer.FromSpan(ref payload);

    var player = _authorizationManager.GetPlayer(peerId);
    if (op == RagonOperation.AUTHORIZE)
    {
      if (player != null)
      {
        _logger.Warn("Player already authorized");
        return;
      }
      
      var key = _serializer.ReadString();
      var playerName = _serializer.ReadString();
      var additionalData = _serializer.ReadData(_serializer.Size);
      _authorizationManager.OnAuthorization(peerId, key, playerName, additionalData);
      return;
    }
    
    if (player == null)
    {
      _logger.Warn($"Peer not authorized {peerId} trying to {op}");
      return;
    }

    switch (op)
    {
      case RagonOperation.JOIN_ROOM:
      {
        var roomId = _serializer.ReadString();
        var exists = _roomManager.Rooms.Any(r => r.Id == roomId);
        if (!exists)
        {
          _serializer.Clear();
          _serializer.WriteOperation(RagonOperation.JOIN_FAILED);
          _serializer.WriteString($"Room with id {roomId} not exists");
          var sendData = _serializer.ToArray();
          _gameThread.SocketServer.Send(peerId, sendData, DeliveryType.Reliable);
          return;
        }

        if (_roomManager.RoomsBySocket.ContainsKey(peerId))
          _roomManager.Left(player, Array.Empty<byte>());

        _roomManager.Join(player, roomId, Array.Empty<byte>());
        break;
      }
      case RagonOperation.CREATE_ROOM:
      {
        var roomId = Guid.NewGuid().ToString();
        var custom = _serializer.ReadBool();
        if (custom)
        {
          roomId = _serializer.ReadString();
          var exists = _roomManager.Rooms.Any(r => r.Id == roomId);
          if (exists)
          {
            _serializer.Clear();
            _serializer.WriteOperation(RagonOperation.JOIN_FAILED);
            _serializer.WriteString($"Room with id {roomId} already exists");
            
            var sendData = _serializer.ToArray();
            _gameThread.SocketServer.Send(peerId, sendData, DeliveryType.Reliable);
            return;
          }
        }
        
        var roomProperties = new RagonRoomParameters();
        roomProperties.Deserialize(_serializer);

        if (_roomManager.RoomsBySocket.ContainsKey(peerId))
          _roomManager.Left(player, Array.Empty<byte>());

        _roomManager.Create(player, roomId, roomProperties, Array.Empty<byte>());
        break;
      }
      case RagonOperation.JOIN_OR_CREATE_ROOM:
      {
        var roomId = Guid.NewGuid().ToString();
        var roomProperties = new RagonRoomParameters();
        roomProperties.Deserialize(_serializer);

        if (_roomManager.RoomsBySocket.ContainsKey(peerId))
          _roomManager.Left(player, Array.Empty<byte>());

        _roomManager.JoinOrCreate(player, roomId, roomProperties, Array.Empty<byte>());
        break;
      }
      case RagonOperation.LEAVE_ROOM:
      {
        _roomManager.Left(player, Array.Empty<byte>());
        break;
      }
    }
  }

  public void OnDisconnected(ushort peerId)
  {
    _authorizationManager.Cleanup(peerId);
  }
}