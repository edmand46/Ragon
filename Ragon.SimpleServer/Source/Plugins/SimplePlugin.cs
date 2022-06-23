using Ragon.Core;

namespace Game.Source
{
  public class SimplePlugin: PluginBase
  {
    public override void OnStart()
    {
      _logger.Info("Plugin started");
    }

    public override void OnStop()
    {
      _logger.Info("Plugin stopped");
    }
   
    public override void OnPlayerJoined(Player player)
    {
      
      _logger.Info($"Player({player.PlayerName}) joined to Room({GameRoom.Id})");
    }

    public override void OnPlayerLeaved(Player player)
    {
      _logger.Info($"Player({player.PlayerName}) left from Room({GameRoom.Id})");
    }
  }
}