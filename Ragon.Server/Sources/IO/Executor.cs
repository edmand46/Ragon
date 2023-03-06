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

using System.Threading.Channels;

namespace Ragon.Server;

public class Executor: TaskScheduler, IExecutor
{
  private ChannelReader<Task> _reader;
  private ChannelWriter<Task> _writer;
  private Queue<Task> _pendingTasks;
  private TaskFactory _taskFactory;

  public void Run(Action action)
  {
    _taskFactory.StartNew(action);
  }

  public Executor()
  {
    var channel = Channel.CreateUnbounded<Task>();
    _reader = channel.Reader;
    _writer = channel.Writer;
    
    _taskFactory = new TaskFactory(this);
    _pendingTasks = new Queue<Task>();
  }

  protected override IEnumerable<Task>? GetScheduledTasks()
  {
    throw new NotSupportedException();
  }

  protected override void QueueTask(Task task)
  {
    _writer.TryWrite(task);
  }

  protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
  {
    return false;
  }

  public void Update()
  {
    while (_reader.TryRead(out var task))
    {
      TryExecuteTask(task);
      
      if (task.Status == TaskStatus.Running)
        _pendingTasks.Enqueue(task);
    }

    while (_pendingTasks.TryDequeue(out var task))
      _writer.TryWrite(task);
  }
}