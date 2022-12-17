namespace Ragon.Server;

public struct NetworkConfiguration
{
  public int LimitConnections { get; set; }
  public int Port { get; set;  }
  public uint Protocol { get; set; }
  public string Address { get; set; }
}