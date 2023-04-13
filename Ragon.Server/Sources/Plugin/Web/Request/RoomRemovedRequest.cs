using Ragon.Server.Plugin.Web;

namespace Ragon.Server.Plugin.Web;

[Serializable]
public class RoomRemovedRequest
{
  public RoomDto Room { get; set; }
}