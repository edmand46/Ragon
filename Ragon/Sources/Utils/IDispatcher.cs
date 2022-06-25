using System;

namespace Ragon.Core;

public interface IDispatcher
{
  public void Dispatch(Action action);
}