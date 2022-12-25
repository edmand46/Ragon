using System.Threading.Channels;

namespace Ragon.Core.Server;

public class Executor: TaskScheduler
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

  public void Execute()
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