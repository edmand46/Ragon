using System;
using System.Collections.Generic;

namespace Ragon.Core;

public class Dispatcher: IDispatcher
{
  public Queue<DispatcherTask> _actions = new Queue<DispatcherTask>();
  public void Dispatch(Action action)
  {
    lock (_actions)
      _actions.Enqueue(new DispatcherTask() { Action = action });
  }

  public void Process()
  {
    lock(_actions)
      while(_actions.TryDequeue(out var action))
        action.Execute();
  }
}