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
  private readonly List<GameRoom> _rooms = new List<GameRoom>();
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
    if (_rooms.Count > 0)
    {
      foreach (var existRoom in _rooms)
      {
        if (existRoom.Id == roomId && existRoom.PlayersCount < existRoom.PlayersMax)
        {
          existRoom.Joined(player, payload);
          _roomsBySocket.Add(player.PeerId, existRoom);
          break;
        }
      }
    }
  }

  public void Create(Player player, string map, int min, int max, byte[] payload)
  {
    var plugin = _factory.CreatePlugin(map);
    if (plugin == null)
      throw new NullReferenceException($"Plugin for map {map} is null");

    var room = new GameRoom(_gameThread, plugin, map, min, max);
    room.Joined(player, payload);
    room.Start();

    _roomsBySocket.Add(player.PeerId, room);
    _rooms.Add(room);
  }

  public void JoinOrCreate(Player player, string map, int min, int max, byte[] payload)
  {
    if (_rooms.Count > 0)
    {
      foreach (var existRoom in _rooms)
      {
        if (existRoom.Map == map && existRoom.PlayersCount < existRoom.PlayersMax)
        {
          existRoom.Joined(player, payload);
          _roomsBySocket.Add(player.PeerId, existRoom);
          return;
        }
      }
    }

    var plugin = _factory.CreatePlugin(map);
    if (plugin == null)
      throw new NullReferenceException($"Plugin for map {map} is null");

    var room = new GameRoom(_gameThread, plugin, map, min, max);
    room.Joined(player, payload);
    room.Start();

    _roomsBySocket.Add(player.PeerId, room);
    _rooms.Add(room);
  }

  public void Left(Player player, byte[] payload)
  {
    if (_roomsBySocket.Remove(player.PeerId, out var room))
    {
      room.Leave(player.PeerId);
      if (room.PlayersCount < room.PlayersMin)
      {
        room.Stop();
        _rooms.Remove(room);
      }
      
      _gameThread.Server.Send(player.PeerId, new byte[] { (byte) RagonOperation.LEAVE_ROOM }, DeliveryType.Reliable);
    }
  }

  public void Tick(float deltaTime)
  {
    foreach (var gameRoom in _rooms)
      gameRoom.Tick(deltaTime);
  }
}