using System;
using NLog;
using Ragon.Server.Logging;

namespace Ragon.Relay;

public class RelayLogger: IRagonLogger
{
  private Logger _nlogger;
  
  public RelayLogger(string tag)
  {
    _nlogger = LogManager.GetLogger(tag);
  }
  
  public void Warning(string message)
  {
    _nlogger.Warn(message);
  }

  public void Info(string message)
  {
    _nlogger.Info(message);
  }

  public void Error(string message)
  {
    _nlogger.Error(message);
  }

  public void Error(Exception ex)
  {
    _nlogger.Error(ex);
  }

  public void Trace(string message)
  {
    _nlogger.Trace(message);
  }
}