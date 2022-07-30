using System;
using System.Collections.Generic;
using NetStack.Serialization;
using NLog;
using Ragon.Common;

namespace Ragon.Core;

public class Lobby : ILobby
{
  private readonly ILogger _logger = LogManager.GetCurrentClassLogger();
  private readonly RagonSerializer _serializer;
  private readonly BitBuffer _buffer;
  private readonly RoomManager _roomManager;
  private readonly AuthorizationManager _authorizationManager;

  public AuthorizationManager AuthorizationManager => _authorizationManager;

  public Lobby(IAuthorizationProvider provider, RoomManager manager, IGameThread gameThread)
  {
    _roomManager = manager;
    _buffer = new BitBuffer();
    _serializer = new RagonSerializer();
    _authorizationManager = new AuthorizationManager(provider, gameThread, this, _serializer);
  }

  public void ProcessEvent(uint peerId, RagonOperation op, ReadOnlySpan<byte> payload)
  {
    _serializer.Clear();
    _serializer.FromSpan(ref payload);

    if (op == RagonOperation.AUTHORIZE)
    {
      var key = _serializer.ReadString();
      var playerName = _serializer.ReadString();
      var protocol = _serializer.ReadByte();
      _authorizationManager.OnAuthorization(peerId, key, playerName, protocol);
      return;
    }

    var player = _authorizationManager.GetPlayer(peerId);
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
          roomId = _serializer.ReadString();

        var propertiesPayload = _serializer.ReadData(_serializer.Size);
        _buffer.Clear();
        _buffer.FromSpan(ref propertiesPayload, propertiesPayload.Length);
        
        var roomProperties = new RagonRoomParameters();
        roomProperties.Deserialize(_buffer);

        if (_roomManager.RoomsBySocket.ContainsKey(peerId))
          _roomManager.Left(player, Array.Empty<byte>());

        _roomManager.Create(player, roomId, roomProperties, Array.Empty<byte>());
        break;
      }
      case RagonOperation.JOIN_OR_CREATE_ROOM:
      {
        var roomId = Guid.NewGuid().ToString();
        var roomProperties = new RagonRoomParameters();
        var propertiesPayload = _serializer.ReadData(_serializer.Size);
        
        _buffer.Clear();
        _buffer.FromSpan(ref propertiesPayload, propertiesPayload.Length);
        
        roomProperties.Deserialize(_buffer);

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

  public void OnDisconnected(uint peerId)
  {
    _authorizationManager.Cleanup(peerId);
  }
}