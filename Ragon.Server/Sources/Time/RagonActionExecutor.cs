/*
 * Copyright 2023 Eduard Kargin <kargin.eduard@gmail.com>
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

namespace Ragon.Server.Time;

public class RagonScheduler
{
  private List<IRagonAction> _tasks;
  
  public RagonScheduler()
  {
    _tasks = new List<IRagonAction>(35);
  }

  public void Run(IRagonAction task)
  {
    _tasks.Add(task);
  }

  public void Stop(IRagonAction task)
  {
    _tasks.Remove(task);
  }

  public void Update(float dt)
  {
    foreach (var task in _tasks)
      task.Tick(dt);
  }
}