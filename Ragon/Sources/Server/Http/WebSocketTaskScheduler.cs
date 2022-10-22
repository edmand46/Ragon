using System;
using System.Collections.Generic;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Ragon.Core;

public class WebSocketTaskScheduler: TaskScheduler
{
  private ChannelReader<Task> _reader;
  private ChannelWriter<Task> _writer;
  private Channel<Task> _channel;

  public WebSocketTaskScheduler()
  {
    _channel = Channel.CreateUnbounded<Task>();
    _reader = _channel.Reader;
    _writer = _channel.Writer;
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

  public void Process()
  {
    while (_reader.TryRead(out var task))
    {
      TryExecuteTask(task);
      
      if (task.Status != TaskStatus.RanToCompletion)
        _writer.TryWrite(task);
    }
  }
}