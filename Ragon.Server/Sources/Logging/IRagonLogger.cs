namespace Ragon.Server.Logging;

public interface IRagonLogger
{
  public void Warning(string tag, string message);
  public void Info(string tag, string message);
  public void Error(string tag, string message);
  public void Trace(string tag, string message);
}