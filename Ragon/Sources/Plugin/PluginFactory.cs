namespace Ragon.Core
{
  public interface PluginFactory
  {
    public PluginBase CreatePlugin(string map);
    public AuthorizationManager CreateManager(Configuration configuration);
  }
  
  
}