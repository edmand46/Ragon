using Ragon.Core;

namespace Game.Source
{
  public class GameFactory : PluginFactory
  {
    public PluginBase CreatePlugin(string map)
    {
      // if (map == "spawn")
      //   return new SpawnPlugin();
      //
      // if (map == "arena")
      //   return new ArenaPlugin();
      return new SpawnPlugin();
    }
  }
}