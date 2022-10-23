using System.Runtime.InteropServices;
using Ragon.Core;

namespace Game.Source
{
  public class SimplePluginFactory : PluginFactory
  {
    public PluginBase CreatePlugin(string map)
    {
      return new SimplePlugin();
    }
    
    public IApplicationHandler CreateAuthorizationProvider(Configuration configuration)
    {
      return new ApplicationHandlerByKey(configuration);
    }
  }
}