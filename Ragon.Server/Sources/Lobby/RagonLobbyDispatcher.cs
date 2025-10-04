using Ragon.Protocol;
using Ragon.Server.Room;

namespace Ragon.Server.Lobby;

public class RagonLobbyDispatcher
{
  private IRagonLobby _lobby;

  public RagonLobbyDispatcher(IRagonLobby lobby)
  {
    _lobby = lobby;
  }

  public void Write(RagonBuffer writer, int projectId = 0)
  {
    writer.Clear();
    writer.WriteOperation(RagonOperation.ROOM_LIST_UPDATED);
    var rooms = _lobby.Rooms;

    if (projectId > 0)
    {
      rooms = rooms.Where(r => r.ProjectId == projectId).ToList();
    }

    writer.WriteUShort((ushort)rooms.Count);
    for (int i = 0; i < rooms.Count; i++)
    {
      var room = rooms[i];

      writer.WriteString(room.Id);
      writer.WriteString(room.Scene);
      writer.WriteUShort((ushort)room.PlayerMax);
      writer.WriteUShort((ushort)room.PlayerMin);
      writer.WriteUShort((ushort)room.PlayerCount);
    }
  }
}