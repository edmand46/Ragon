using System;
using System.Collections.Generic;
using Ragon.Common;
namespace Ragon.Core;

public class Lobby
{
  private readonly RagonSerializer _serializer;
  private readonly AuthorizationManager _authorizationManager;
  private readonly RoomManager _roomManager;

  public Lobby(IAuthorizationProvider provider, RoomManager manager, IGameThread gameThread)
  {
    _roomManager = manager;
    _serializer = new RagonSerializer();
    _authorizationManager = new AuthorizationManager(provider, gameThread, this, _serializer);
  }

  public void ProcessEvent(uint peerId, ReadOnlySpan<byte> data)
  {
    var op = (RagonOperation) data[0];
    var payload = data.Slice(1, data.Length - 1);

    _serializer.Clear();
    _serializer.FromSpan(ref payload);

    switch (op)
    {
      case RagonOperation.AUTHORIZE:
      {
        var key = _serializer.ReadString();
        var playerName = _serializer.ReadString();
        var protocol = _serializer.ReadByte();
        _authorizationManager.OnAuthorization(peerId, key, playerName, protocol);
        break;
      }
      case RagonOperation.JOIN_ROOM:
      {
        var roomId = _serializer.ReadString();
        var player = _authorizationManager.GetPlayer(peerId);
        _roomManager.Join(player, roomId, Array.Empty<byte>());
        break;
      }
      case RagonOperation.JOIN_OR_CREATE_ROOM:
      {
        var map = _serializer.ReadString();
        var min = _serializer.ReadInt();
        var max = _serializer.ReadInt();
        var player = _authorizationManager.GetPlayer(peerId);
        _roomManager.JoinOrCreate(player, map, min, max, Array.Empty<byte>());
        break;
      }
      case RagonOperation.LEAVE_ROOM:
      {
        var player = _authorizationManager.GetPlayer(peerId);
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