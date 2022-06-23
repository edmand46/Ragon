using System;
using System.Diagnostics;

namespace Ragon.Core;

public class DispatcherTask
{
  public Action Action;
  public Action Callback;

  public void Execute()
  {
    Action?.Invoke();
  }
}