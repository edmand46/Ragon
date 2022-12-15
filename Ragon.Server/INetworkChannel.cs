namespace Ragon.Server;

public interface INetworkChannel
{
  void Send(byte[] data);
}