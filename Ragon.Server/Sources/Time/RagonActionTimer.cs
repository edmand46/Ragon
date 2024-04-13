using Ragon.Server.Time;

public class RagonActionTimer: IRagonAction
{
  private Action _callback;
  private float _timer;
  private float _time;
  
  public RagonActionTimer(Action callback, float timeInSeconds)
  {
    _callback = callback;
    _time = timeInSeconds * 1000;
  }

  public void Tick(float dt)
  {
    _timer += dt;
    if (_timer >= _time)
      _callback?.Invoke();
  }
}