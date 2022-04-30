namespace Ragon.Core
{
  public interface PluginFactory
  {
    public string PluginName { get; set; }
    public PluginBase CreatePlugin(string map);
    public AuthorizationManager CreateManager();
  }
  
  
}