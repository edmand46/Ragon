namespace Ragon.Core;

public interface ISender<T>
{
  public void Send(T data);
}