using System;
using Ragon.Server.Entity;
using Ragon.Server.Plugin;
using Ragon.Server.Room;

namespace Ragon.Relay;

public class RelayRoomPlugin: BaseRoomPlugin
{
  public void Tick(float dt)
  {
    
  }

  public void OnAttached()
  {
    Console.WriteLine("Room attached");
  }

  public void OnDetached()
  {
    Console.WriteLine("Room detached");
  }

  public bool OnEntityCreate(RagonRoomPlayer creator, RagonEntity entity)
  {
    Console.WriteLine($"Entity created: {entity.Id}");
    return true;
  }

  public bool OnEntityRemove(RagonRoomPlayer destroyer, RagonEntity entity)
  {
    Console.WriteLine($"Entity destroyed: {entity.Id}");
    return true;
  }

  public override bool OnData(RagonRoomPlayer player, byte[] data)
  {
    Console.WriteLine("Data received");
    return true;
  }
}