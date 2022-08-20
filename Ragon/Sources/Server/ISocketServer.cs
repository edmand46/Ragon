namespace Ragon.Core;

public interface ISocketServer
{
  public void Start(ushort port, int connections, uint protocol);
  public void Process();
  public void Stop();
  public void Send(uint peerId, byte[] data, DeliveryType type);
  public void Broadcast(uint[] peersIds, byte[] data, DeliveryType type);
  public void Disconnect(uint peerId, uint errorCode);
}