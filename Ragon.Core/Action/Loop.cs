namespace Ragon.Core.Time;

public class Loop
{
  private List<IAction> _tasks;

  public Loop()
  {
    
    _tasks = new List<IAction>(35);
  }

  public void Run(IAction task)
  {
    _tasks.Add(task);
  }

  public void Stop(IAction task)
  {
    _tasks.Remove(task);
  }

  public void Tick()
  {
    foreach (var task in _tasks)
      task.Tick();
  }
}