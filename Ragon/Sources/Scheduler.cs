using System;
using System.Collections.Generic;


namespace Ragon.Core
{
  public class Scheduler: IDisposable
  {
    List<SchedulerTask> _scheduledTasks;

    public Scheduler(int defaultCapacity = 100)
    {
      _scheduledTasks = new List<SchedulerTask>(defaultCapacity);
    }
    
    public SchedulerTask Schedule(Action<SchedulerTask> action, float interval, int count = 1)
    {
      var newTask = new SchedulerTask(action, interval, count - 1);
      _scheduledTasks.Add(newTask);
      return newTask;
    }

    public SchedulerTask ScheduleForever(Action<SchedulerTask> action, float interval)
    {
      var newTask = new SchedulerTask(action, interval, -1);
      _scheduledTasks.Add(newTask);
      return newTask;
    }

    public void StopSchedule(SchedulerTask schedulerTask)
    {
      if (_scheduledTasks.Contains(schedulerTask))
        _scheduledTasks.Remove(schedulerTask);
    }

    public void Tick(float deltaTime)
    {
      for (int i = _scheduledTasks.Count - 1; i >= 0; i--)
      {
        var scheduledTask = _scheduledTasks[i];
        scheduledTask.Tick(deltaTime);
        
        if (!scheduledTask.IsActive)
          _scheduledTasks.Remove(scheduledTask);
      }
    }

    public void Dispose()
    {
      _scheduledTasks.Clear();
    }
  }

  public class SchedulerTask
  {
    private Action<SchedulerTask> _action;
    private float _timer = 0;
    private float _interval = 0;
    private int _repeats = 0;
    private bool _active;
    
    public int Repeats => _repeats;
    public bool IsActive => _active;
    
    public SchedulerTask(Action<SchedulerTask> task, float interval, int repeatCount = 0)
    {
      _action = task;
      _interval = interval;
      _timer = 0;
      _active = true;
      _repeats = repeatCount;
    }

    public void Tick(float deltaTime)
    {
      _timer += deltaTime;
      if (_timer >= _interval)
      {
        _action.Invoke(this);
        if (_repeats == -1)
        {
          _timer = 0;
          return;
        }

        if (_repeats > 0)
        {
          _timer = 0;
          _repeats--;
          return;
        }

        if (_repeats == 0)
          _active = false;
      }
    }
  }
}