namespace Ragon.Server.Plugin.Web;

[Serializable]
public class RoomLeavedRequest
{
  public RoomDto Room { get; set; }
  public PlayerDto Player { get; set; }
}