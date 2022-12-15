namespace Ragon.Core.Time;

public class Scheduler
{
  private List<IScheduleTask> _tasks;

  public Scheduler()
  {
    _tasks = new List<IScheduleTask>(35);
  }

  public void Add(IScheduleTask task)
  {
    _tasks.Add(task);
  }

  public void Remove(IScheduleTask task)
  {
    _tasks.Remove(task);
  }

  public void Tick()
  {
    foreach (var task in _tasks)
      task.Tick();
  }
}