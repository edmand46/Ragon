using System.Net.Http;
using Ragon.Server;
using Ragon.Server.Lobby;
using Ragon.Server.Plugin;
using Ragon.Server.Room;

namespace Ragon.Relay;

public class RelayServerPlugin: IServerPlugin
{
  private HttpClient httpClient;
  public IRoomPlugin CreateRoomPlugin(RoomInformation information)
  {
    return new RelayRoomPlugin();
  }

  public RelayServerPlugin()
  {
    httpClient = new HttpClient();
  }

  public bool OnRoomCreate(RagonLobbyPlayer player, RagonRoom room)
  {
    return true;
  }

  public bool OnRoomRemove(RagonLobbyPlayer player, RagonRoom room)
  {
    return true;
  }

  public bool OnRoomLeave(RagonRoomPlayer player, RagonRoom room)
  {
    return true;
  }

  public bool OnRoomJoin(RagonRoomPlayer player, RagonRoom room)
  {
    return true;
  }
}