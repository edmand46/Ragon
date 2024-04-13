namespace Ragon.Server.Logging;

public interface IRagonLoggerFactory
{
  public IRagonLogger GetLogger(string tag);
}