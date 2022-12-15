using System.Collections.Generic;
using NLog;
using Ragon.Core.Game;

namespace Ragon.Core.Lobby;

public class LobbyInMemory: ILobby
{
  private readonly List<Room> _rooms = new();
  private readonly Logger _logger = LogManager.GetCurrentClassLogger();
  
  public bool FindRoomById(string roomId, out Room room)
  {
    foreach (var existRoom in _rooms)
    {
      var info = existRoom.Info;
      if (existRoom.Id == roomId && info.Min < info.Max)
      {
        room = existRoom;
        return true;
      }
    }

    room = null;
    return false;
  }

  public bool FindRoomByMap(string map, out Room room)
  {
    foreach (var existRoom in _rooms)
    {
      var info = existRoom.Info;
      if (info.Map == map && existRoom.Players.Count < info.Max)
      {
        room = existRoom;
        return true;
      }
    }

    room = null;
    return false;
  }

  public void Persist(Room room)
  {
    _rooms.Add(room);

    foreach (var r in _rooms)
      _logger.Trace($"{r.Id} {r.Info}");
  }

  public void Remove(Room room)
  {
    _rooms.Remove(room);
  }
}