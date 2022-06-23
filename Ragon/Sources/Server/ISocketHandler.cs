using ENet;

namespace Ragon.Core;

public interface IHandler
{
  public void OnEvent(Event evnt);
}