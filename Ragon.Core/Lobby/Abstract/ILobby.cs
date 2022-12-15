using Ragon.Core.Game;

namespace Ragon.Core.Lobby;

public interface ILobby
{
  public bool FindRoomById(string roomId, out Room room);
  public bool FindRoomByMap(string map, out Room room);
  public void Persist(Room room);
  public void Remove(Room room);
}