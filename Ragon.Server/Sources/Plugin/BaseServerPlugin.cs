using Ragon.Server.IO;
using Ragon.Server.Lobby;
using Ragon.Server.Room;

namespace Ragon.Server.Plugin;

public class BaseServerPlugin: IServerPlugin
{
  private IRagonServer _ragonServer;
  
  public RagonLobbyPlayer? GetPlayerById(string id)
  {
    var context = _ragonServer.ResolveContext(id);
    return context?.LobbyPlayer;
  }

  public RagonLobbyPlayer? GetPlayerByConnection(INetworkConnection connection)
  {
    var context = _ragonServer.ResolveContext(connection);
    return context?.LobbyPlayer;
  }

  public void OnAttached(IRagonServer server)
  {
    _ragonServer = server;
  }

  public void OnDetached()
  {
    
  }

  public virtual bool OnRoomCreate(RagonLobbyPlayer player, RagonRoom room)
  {
    return true;
  }

  public virtual bool OnRoomRemove(RagonLobbyPlayer player, RagonRoom room)
  {
    return true;
  }

  public virtual bool OnRoomLeave(RagonRoomPlayer player, RagonRoom room)
  {
    return true;
  }

  public virtual bool OnRoomJoin(RagonRoomPlayer player, RagonRoom room)
  {
    return true;
  }

  public virtual bool OnCommand(string command, string payload)
  {
    return true;
  }

  public IRoomPlugin CreateRoomPlugin(RoomInformation information)
  {
    return new BaseRoomPlugin();
  }
}