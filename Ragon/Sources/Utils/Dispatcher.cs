using System;
using System.Collections.Generic;

namespace Ragon.Core;

public class Dispatcher: IDispatcher, IDispatcherInternal
{
  public Queue<Action> _actions = new Queue<Action>();
  
  public void Dispatch(Action action)
  {
    lock (_actions)
      _actions.Enqueue(action);
  }

  public void Process()
  {
    lock(_actions)
      while(_actions.TryDequeue(out var action))
        action?.Invoke();
  }
}