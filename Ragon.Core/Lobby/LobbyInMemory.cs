using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using NLog;
using Ragon.Core.Game;

namespace Ragon.Core.Lobby;

public class LobbyInMemory : ILobby
{
  private readonly List<Room> _rooms = new();
  private readonly Logger _logger = LogManager.GetCurrentClassLogger();

  public bool FindRoomById(string roomId, [MaybeNullWhen(false)] out Room room)
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

    room = default;
    return false;
  }

  public bool FindRoomByMap(string map, [MaybeNullWhen(false)] out Room room)
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

    room = default;
    return false;
  }

  public void Persist(Room room)
  {
    _rooms.Add(room);

    foreach (var r in _rooms)
      _logger.Trace($"Room: {r.Id} {r.Info} Players: {r.Players.Count}");
  }

  public void RemoveIfEmpty(Room room)
  {
    if (room.Players.Count == 0)
      _rooms.Remove(room);
    
    foreach (var r in _rooms)
      _logger.Trace($"Room: {r.Id} {r.Info} Players: {r.Players.Count}");
  }
}