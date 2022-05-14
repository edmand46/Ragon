using System;

namespace Ragon.Core
{
  [Serializable]
  public struct Server 
  {
    public ushort Port;
    public ushort TickRate;
  }
  
  [Serializable]
  public struct Configuration
  {
    public string Key;
    public Server Server;
  }
}