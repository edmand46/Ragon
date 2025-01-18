using System;
using Ragon.Server.Plugin;

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
}