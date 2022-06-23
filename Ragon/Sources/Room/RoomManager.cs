
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
  private List<GameRoom> _rooms = new List<GameRoom>();

  public RoomManager(PluginFactory factory, IGameThread gameThread)
  {
    _gameThread = gameThread;
    _factory = factory;
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
          _gameThread.Attach(player.PeerId, existRoom);
          break;
        }
      }
    }
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
          _gameThread.Attach(player.PeerId, existRoom);
          
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

    _gameThread.Attach(player.PeerId, room);
    _rooms.Add(room);
  }

  public void Left(Player player, byte[] payload)
  {
    
  }

  public void Tick(float deltaTime)
  {
    foreach (var gameRoom in _rooms)
      gameRoom.Tick(deltaTime);
  }
}