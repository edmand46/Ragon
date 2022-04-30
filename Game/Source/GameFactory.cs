using Ragon.Core;

namespace Game.Source
{
  public class GameFactory : PluginFactory
  {
    public string PluginName { get; set; }  = "ExamplePlugin";
    public PluginBase CreatePlugin(string map) => new ExamplePlugin();
    public AuthorizationManager CreateManager() => new GameAuthorizer();
  }
}