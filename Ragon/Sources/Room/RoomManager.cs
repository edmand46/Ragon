using System;
using System.Collections.Generic;
using System.Text;
using NLog;
using Ragon.Common;

namespace Ragon.Core;

public class RoomManager
{
  private readonly IGameThread _gameThread;
  private readonly PluginFactory _factory;
  private readonly Logger _logger = LogManager.GetCurrentClassLogger();
  private readonly List<GameRoom> _rooms = new();
  private readonly Dictionary<uint, GameRoom> _roomsBySocket;

  public IReadOnlyDictionary<uint, GameRoom> RoomsBySocket => _roomsBySocket;
  public IReadOnlyList<GameRoom> Rooms => _rooms;
  
  public RoomManager(PluginFactory factory, IGameThread gameThread)
  {
    _gameThread = gameThread;
    _factory = factory;
    _roomsBySocket = new Dictionary<uint, GameRoom>();
  }

  public void Join(Player player, string roomId, byte[] payload)
  {
    _logger.Trace($"Player ({player.PlayerName}|{player.Id}) joined to room with Id {roomId}");
    
    if (_rooms.Count > 0)
    {
      foreach (var existRoom in _rooms)
      {
        if (existRoom.Id == roomId && existRoom.PlayersCount < existRoom.PlayersMax)
        {
          existRoom.AddPlayer(player, payload);
          _roomsBySocket.Add(player.PeerId, existRoom);
          return;
        }
      }
    }
  }

  public void Create(Player creator, string roomId, RagonRoomParameters parameters, byte[] payload)
  {
    var map = parameters.Map;
    var min = parameters.Min;
    var max = parameters.Max;
    
    _logger.Trace($"Player ({creator.PlayerName}|{creator.Id}) create room with Id {roomId} and params ({map}|{min}|{max})");

    var plugin = _factory.CreatePlugin(map);
    if (plugin == null)
      throw new NullReferenceException($"Plugin for map {map} is null");

    var room = new GameRoom(_gameThread, plugin, roomId, map, min, max);
    room.AddPlayer(creator, payload);
    room.Start();

    _roomsBySocket.Add(creator.PeerId, room);
    _rooms.Add(room);
  }

  public void JoinOrCreate(Player player, string roomId, RagonRoomParameters parameters, byte[] payload)
  {
    var map = parameters.Map;
    var min = parameters.Min;
    var max = parameters.Max;
    
    if (_rooms.Count > 0)
    {
      foreach (var existRoom in _rooms)
      {
        if (existRoom.Map == map && existRoom.PlayersCount < existRoom.PlayersMax)
        {
          _logger.Trace($"Player ({player.PlayerName}|{player.Id}) joined to room with Id {roomId}");
          
          existRoom.AddPlayer(player, payload);
          _roomsBySocket.Add(player.PeerId, existRoom);
          return;
        }
      }
    }

    _logger.Trace($"Room not found for Player ({player.PlayerName}|{player.Id}), create room with Id {roomId} and params ({map}|{min}|{max})");
    
    var plugin = _factory.CreatePlugin(map);
    if (plugin == null)
      throw new NullReferenceException($"Plugin for map {map} is null");

    var room = new GameRoom(_gameThread, plugin, roomId, map, min, max);
    room.AddPlayer(player, payload);
    room.Start();

    _roomsBySocket.Add(player.PeerId, room);
    _rooms.Add(room);
  }

  public void Left(Player player, byte[] payload)
  {
    if (_roomsBySocket.Remove(player.PeerId, out var room))
    {
      _logger.Trace($"Player ({player.PlayerName}|{player.Id}) left room with Id {room.Id}");
      room.RemovePlayer(player.PeerId);
      if (room.PlayersCount < room.PlayersMin)
      {
        _logger.Trace($"Room with Id {room.Id} destroyed");
        room.Stop();
        _rooms.Remove(room);
      }

      _gameThread.Server.Send(player.PeerId, new byte[] {(byte) RagonOperation.LEAVE_ROOM}, DeliveryType.Reliable);
    }
  }

  public void Tick(float deltaTime)
  {
    foreach (var gameRoom in _rooms)
      gameRoom.Tick(deltaTime);
  }
}