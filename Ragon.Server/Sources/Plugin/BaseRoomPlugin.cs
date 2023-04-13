using Ragon.Server.Entity;
using Ragon.Server.IO;
using Ragon.Server.Lobby;
using Ragon.Server.Room;
using Ragon.Server.Time;

namespace Ragon.Server.Plugin;

public class BaseRoomPlugin: IRoomPlugin
{
  private IRagonRoom _ragonRoom;
  
  public RagonRoomPlayer GetPlayerById(string id)
  {
    var player = _ragonRoom.GetPlayerById(id);
    return player;
  }

  public RagonRoomPlayer GetPlayerByConnection(INetworkConnection connection)
  {
    var player = _ragonRoom.GetPlayerByConnection(connection);
    return player;
  }
  
  public virtual void OnAttached(IRagonRoom room)
  {
    _ragonRoom = room;
  }
  
  public virtual void OnDetached()
  {
    
  }

  #region VIRTUAL
  
  public virtual void Tick(float dt)
  {
    
  }
  
  public virtual bool OnEntityCreate(RagonRoomPlayer creator, RagonEntity entity)
  {
    return true;
  }

  public virtual bool OnEntityRemove(RagonRoomPlayer remover, RagonEntity entity)
  {
    return true;
  }
  
  #endregion
}