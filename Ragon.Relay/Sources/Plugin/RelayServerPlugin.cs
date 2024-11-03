using Ragon.Server;
using Ragon.Server.Plugin;

namespace Ragon.Relay
{
  public class RelayServerPlugin : BaseServerPlugin
  {
    public override bool OnCommand(string command, string payload)
    {
      return true;
    }

    public override IRoomPlugin CreateRoomPlugin(RoomInformation information)
    {
      return new RelayRoomPlugin();
    }
  }
}