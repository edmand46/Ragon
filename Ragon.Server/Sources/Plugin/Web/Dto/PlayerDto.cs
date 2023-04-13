using Ragon.Server.Room;

namespace Ragon.Server.Plugin.Web;

[Serializable]
public class PlayerDto
{
  public string Id { get; set;}
  public string Name { get; set; }
  
  public PlayerDto(RagonRoomPlayer ragonRoomPlayer)
  {
    Id = ragonRoomPlayer.Id;
    Name = ragonRoomPlayer.Name;
  }
}