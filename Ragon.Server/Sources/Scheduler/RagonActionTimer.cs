/*
 * Copyright 2023-2024 Eduard Kargin <kargin.eduard@gmail.com>
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */


using Ragon.Server.Time;

public class RagonActionTimer : IRagonAction
{
  public bool IsDone => _repeatCount == 0;
  
  private Action _callback;
  private float _timer;
  private float _time;
  private float _repeatCount;

  public RagonActionTimer(Action callback, float timeInSeconds, int repeat = 1)
  {
    _callback = callback;
    _time = timeInSeconds * 1000;
    _repeatCount = repeat;
  }

  public void Tick(float dt)
  {
    _timer += dt;
    if (_timer >= _time)
    {
      _callback?.Invoke();
      _timer = 0;
    }
  }
}