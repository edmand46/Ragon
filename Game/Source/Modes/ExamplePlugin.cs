using NLog;
using Ragon.Core;

namespace Game.Source
{
  public class ExamplePlugin: PluginBase
  {
    private ILogger _logger = LogManager.GetCurrentClassLogger();

    public override void OnStart()
    {
      _logger.Info("Plugin started");  
    }

    public override void OnStop()
    {
      _logger.Info("Plugin stopped");
    }
  }
}