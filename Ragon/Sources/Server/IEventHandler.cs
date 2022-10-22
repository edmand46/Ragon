using ENet;

namespace Ragon.Core;

public interface IEventHandler
{
  public void OnConnected(ushort peerId);
  public void OnDisconnected(ushort peerId);
  public void OnTimeout(ushort peerId);
  public void OnData(ushort peerId, byte[] data);
}