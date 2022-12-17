namespace Ragon.Server;

public interface INetworkListener
{
  void OnConnected(INetworkConnection connection);
  void OnDisconnected(INetworkConnection connection);
  void OnTimeout(INetworkConnection connection);
  void OnData(INetworkConnection connection, byte[] data);
}