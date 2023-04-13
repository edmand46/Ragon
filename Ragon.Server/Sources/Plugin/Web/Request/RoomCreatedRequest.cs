
namespace Ragon.Server.Plugin.Web;

[Serializable]
public class RoomCreatedRequest
{
  public RoomDto Room { get; set; }
  public PlayerDto Player { get; set; }
}