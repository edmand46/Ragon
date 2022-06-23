using System;

namespace Ragon.Core
{
  [Serializable]
  public struct Server 
  {
    public ushort Port;
  }
  
  [Serializable]
  public struct Configuration
  {
    public string Key;
    public ushort TickRate;
    public Server Server;
  }
}