namespace Ragon.Server.Logging;

public interface IRagonLogger
{
  public void Warning(string message);
  public void Info(string message);
  public void Error(string message);
  public void Error(Exception ex);
  public void Trace(string message);
}