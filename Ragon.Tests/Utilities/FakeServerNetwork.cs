using Ragon.Server.IO;

namespace Ragon.Tests;

public class FakeServerNetwork: INetworkServer
{
  public void Close()
  {
    
  }

  public void Stop()
  {
    
  }

  public void Update()
  {
  }

  public void Broadcast(byte[] data, NetworkChannel channel)
  {
  }

  public void Listen(INetworkListener listener, NetworkConfiguration configuration)
  {
  }
}