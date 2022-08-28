using NLog.Fluent;
using Ragon.Core;

namespace Game.Source
{
  public class SimplePlugin: PluginBase
  {
    
    public override void OnStart()
    {
      // _logger.Info("Plugin started");
    }

    public override void OnStop()
    {
      // _logger.Info("Plugin stopped");
    }
   
    public override void OnPlayerJoined(Player player)
    {
      // Logger.Info($"Player({player.PlayerName}) joined to Room({Room.Id})");
    }

    public override void OnPlayerLeaved(Player player)
    {
      // Logger.Info($"Player({player.PlayerName}) left from Room({Room.Id})");
    }

    public override void OnEntityCreated(Player player, Entity entity)
    {
      // Logger.Info($"Player({player.PlayerName}) create entity {entity.EntityId}:{entity.EntityType}"); 
    }

    public override void OnEntityDestroyed(Player player, Entity entity)
    {
      // Logger.Info($"Player({player.PlayerName}) destroy entity {entity.EntityId}:{entity.EntityType}");
    }
  }
}