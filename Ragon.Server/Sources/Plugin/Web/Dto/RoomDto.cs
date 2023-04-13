using Ragon.Server.Room;

namespace Ragon.Server.Plugin.Web;

[Serializable]
public class RoomDto
{
  public string Id { get; set;}
  public int PlayerMin { get; set; }
  public int PlayerMax { get; set; }
  public int PlayerCount { get; set; }
  public PlayerDto[] Players { get; set; }

  public RoomDto(RagonRoom room)
  {
    Id = room.Id;
    PlayerMin = room.PlayerMin;
    PlayerMax = room.PlayerMax;
    PlayerCount = room.PlayerCount;

    Players = room.PlayerList.Select(p => new PlayerDto(p)).ToArray();
  }
}