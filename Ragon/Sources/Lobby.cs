using System;
using System.Linq;
using NLog;
using Ragon.Common;

namespace Ragon.Core;

public class Lobby
{
  private readonly ILogger _logger = LogManager.GetCurrentClassLogger();
  private readonly Application _application;
  private readonly RagonSerializer _writer;
  private readonly RoomManager _roomManager;
  private readonly AuthorizationManager _authorizationManager;

  public AuthorizationManager AuthorizationManager => _authorizationManager;

  public Lobby(IApplicationHandler provider, RoomManager manager, Application application)
  {
    _roomManager = manager;
    _application = application;
    _writer = new RagonSerializer();
    _authorizationManager = new AuthorizationManager(provider, application, this, _writer);
  }

  public void ProcessEvent(ushort peerId, RagonOperation op, RagonSerializer reader)
  {
    var player = _authorizationManager.GetPlayer(peerId);
    if (op == RagonOperation.AUTHORIZE)
    {
      if (player != null)
      {
        _logger.Warn("Player already authorized");
        return;
      }
      
      var key = reader.ReadString();
      var playerName = reader.ReadString();
      var additionalData = reader.ReadData(reader.Size);
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
        var roomId = reader.ReadString();
        var exists = _roomManager.Rooms.Any(r => r.Id == roomId);
        if (!exists)
        {
          _writer.Clear();
          _writer.WriteOperation(RagonOperation.JOIN_FAILED);
          _writer.WriteString($"Room with id {roomId} not exists");
          var sendData = _writer.ToArray();
          _application.SocketServer.Send(peerId, sendData, DeliveryType.Reliable);
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
        var custom = reader.ReadBool();
        if (custom)
        {
          roomId = reader.ReadString();
          var exists = _roomManager.Rooms.Any(r => r.Id == roomId);
          if (exists)
          {
            _writer.Clear();
            _writer.WriteOperation(RagonOperation.JOIN_FAILED);
            _writer.WriteString($"Room with id {roomId} already exists");
            
            var sendData = _writer.ToArray();
            _application.SocketServer.Send(peerId, sendData, DeliveryType.Reliable);
            return;
          }
        }
        
        var roomProperties = new RagonRoomParameters();
        roomProperties.Deserialize(reader);

        if (_roomManager.RoomsBySocket.ContainsKey(peerId))
          _roomManager.Left(player, Array.Empty<byte>());

        _roomManager.Create(player, roomId, roomProperties, Array.Empty<byte>());
        break;
      }
      case RagonOperation.JOIN_OR_CREATE_ROOM:
      {
        var roomId = Guid.NewGuid().ToString();
        var roomProperties = new RagonRoomParameters();
        roomProperties.Deserialize(reader);

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