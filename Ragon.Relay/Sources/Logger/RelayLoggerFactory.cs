using Ragon.Server.Logging;

namespace Ragon.Relay;

public class RelayLoggerFactory: IRagonLoggerFactory
{
  public IRagonLogger GetLogger(string tag)
  {
    return new RelayLogger(tag);
  }
}