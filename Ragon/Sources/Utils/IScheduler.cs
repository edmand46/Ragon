using System;

namespace Ragon.Core;

public interface IScheduler
{
  public SchedulerTask Schedule(Action<SchedulerTask> action, float interval, int count = 1);
  public SchedulerTask ScheduleForever(Action<SchedulerTask> action, float interval);
  public void StopSchedule(SchedulerTask schedulerTask);

  public void Tick(float deltaTime);
}