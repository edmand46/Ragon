

namespace Ragon.Tests;

public class FakeNetwork
{
  public FakeClientNetwork ClientNetwork;
  public FakeServerNetwork ServerNetwork;
  public FakeSocket Socket;
  
  public FakeNetwork()
  {
    ClientNetwork = new FakeClientNetwork();
    ServerNetwork = new FakeServerNetwork();
  }
}