namespace Ragon.Core
{
  public interface PluginFactory
  {
    public PluginBase CreatePlugin(string map);
  }
}