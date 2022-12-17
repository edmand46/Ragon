namespace Ragon.Server;

public interface INetworkServer
{
  public void Stop();
  public void Poll();
  public void Start(INetworkListener listener, NetworkConfiguration configuration);
}