using Ragon.Core;

namespace Game.Source
{
  public class GameFactory : PluginFactory
  {
    public string PluginName { get; set; }  = "ExamplePlugin";
    public PluginBase CreatePlugin(string map)
    {
      return new ExamplePlugin();
    }

    public AuthorizationManager CreateManager(Configuration configuration) => new GameAuthorizer(configuration);
  }
}