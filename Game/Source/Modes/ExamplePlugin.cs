using System.Runtime.InteropServices;
using Game.Source.Events;
using NLog;
using Ragon.Core;

namespace Game.Source
{
  public class ExamplePlugin: PluginBase
  {
    public override void OnStart()
    {
      _logger.Info("Plugin started");  
      
      Subscribe<TestEvent>(123, OnTestEvent);
    }

    public override void OnStop()
    {
      _logger.Info("Plugin stopped");
    }
    
    private void OnTestEvent(Player player, TestEvent myEvent)
    {
      _logger.Info("Data " + myEvent.TestData);
    }

    public override void OnPlayerJoined(Player player)
    {
      _logger.Info("Player joined " + player.PlayerName);
      SendEvent(player, 123, new TestEvent() { TestData =  "asdf"});

      SendEvent(123, new TestEvent()
      {
        TestData = "Hello!",
      });
    }

    public override void OnPlayerLeaved(Player player)
    {
      _logger.Info("Player leaved " + player.PlayerName);
    }

    public override void OnEntityCreated(Player creator, Entity entity)
    {
      // entity.
      Subscribe<TestEvent>(entity, 123, OnEntityTestEvent);
    }
    
    public override void OnEntityDestroyed(Player destoyer, Entity entity)
    {
      
    }
    
    private void OnEntityTestEvent(Player arg1, int arg2, TestEvent arg3)
    {
      
    }
  }
}