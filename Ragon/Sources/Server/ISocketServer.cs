namespace Ragon.Core;

public interface ISocketServer
{
  public void Start(ushort port);
  public void Process();
  public void Stop();
  public void Send(uint peerId, byte[] data, DeliveryType type);
  public void Disconnect(uint peerId, uint errorCode);
}