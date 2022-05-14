using System.Runtime.InteropServices;
using Ragon.Core;

namespace Game.Source
{
  public class SimplePluginFactory : PluginFactory
  {
    public string PluginName { get; set; }  = "SimplePlugin";
    public PluginBase CreatePlugin(string map)
    {
      
      return new SimplePlugin();
    }

    public AuthorizationManager CreateManager(Configuration configuration)
    {
      return new AuthorizerByKey(configuration);
    }
  }
}