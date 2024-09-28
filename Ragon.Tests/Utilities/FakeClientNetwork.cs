using Ragon.Client;
using Ragon.Protocol;

namespace Ragon.Tests;

public class FakeClientNetwork: INetworkConnection
{
  public void Close()
  {
    throw new NotImplementedException();
  }

  public INetworkChannel Reliable { get; }
  public INetworkChannel Unreliable { get; }
  public Action<byte[]> OnData { get; set; }
  public Action OnConnected { get; set; }
  public Action<RagonDisconnect> OnDisconnected { get; set; }
  public ulong BytesSent { get; }
  public ulong BytesReceived { get; }
  public int Ping { get; }
  public void Prepare()
  {
    throw new NotImplementedException();
  }

  public void Connect(string address, ushort port, uint protocol)
  {
    
  }

  public void Disconnect()
  {

  }

  public void Update()
  {
    
  }

  public void Dispose()
  {
    
  }
}