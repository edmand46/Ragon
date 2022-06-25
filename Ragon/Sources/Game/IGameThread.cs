using Ragon.Common;

namespace Ragon.Core;

public interface IGameThread
{
  public IDispatcher ThreadDispatcher { get; }
  public ISocketServer Server { get; }
}