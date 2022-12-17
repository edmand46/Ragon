using Ragon.Server;

namespace Ragon.Core.Game;

public class RoomPlayer
{
  public INetworkConnection Connection { get; }
  public string Id { get; }
  public string Name { get; }
  public bool IsLoaded { get; private set; }
  public Room Room { get; private set; }
  public EntityList Entities { get; private set; }
  
  public RoomPlayer(INetworkConnection connection, string id, string name)
  {
    Id = id;
    Name = name;
    Connection = connection;
    Entities = new EntityList();
  }

  public void Attach(Room room)
  {
    Room = room;
  }

  public void Detach()
  {
    Room = null!;
  }

  public void SetReady()
  {
    IsLoaded = true;
  }
}