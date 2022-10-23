namespace Ragon.Core;

public interface ISocketServer
{
  public void Start(ushort port, int connections, uint protocol);
  public void Process();
  public void Stop();
  public void Send(ushort peerId, byte[] data, DeliveryType type);
  public void Broadcast(ushort[] peersIds, byte[] data, DeliveryType type);
  public void Disconnect(ushort peerId, uint errorCode);
}