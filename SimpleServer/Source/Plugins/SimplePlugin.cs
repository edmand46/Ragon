using System.Runtime.InteropServices;
using Game.Source.Events;
using NLog;
using Ragon.Common;
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
      _logger.Info("Player joined " + player.PlayerName);
    }

    public override void OnPlayerLeaved(Player player)
    {
      _logger.Info("Player leaved " + player.PlayerName);
    }
  }
}