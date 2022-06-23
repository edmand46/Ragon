namespace Ragon.Core;

public interface Receiver<T>
{
  public bool Receive(out T data);
}