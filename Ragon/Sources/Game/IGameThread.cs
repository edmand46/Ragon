using Ragon.Common;

namespace Ragon.Core;

public interface IGameThread
{
  public void Attach(uint peerId, GameRoom room);
  public void Detach(uint peerId);
  public void SendSocketEvent(SocketEvent socketEvent);
  public IDispatcher GetDispatcher();
}