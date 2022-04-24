using Game.Source.Events;
using NLog;
using Ragon.Core;

namespace Game.Source
{
  public class SpawnPlugin: PluginBase
  {
    private ILogger _logger = LogManager.GetCurrentClassLogger();
    
    public void SpawnEvent(SpawnEvent evnt)
    {
      
    }
    public override void OnStart()
    {
      Subscribe<SpawnEvent>(SpawnEvent);
      
      _logger.Info("Plugin started");  
    }

    public override void OnStop()
    {
      _logger.Info("Plugin stopped");
    }
  }
}