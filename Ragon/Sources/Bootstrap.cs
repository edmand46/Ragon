using NLog;

namespace Ragon.Core
{
  public class Bootstrap
  {
    private ILogger _logger = LogManager.GetCurrentClassLogger();

    public void Configure(PluginFactory factory)
    {
      _logger.Info("Configure application...");
      var configuration = ConfigurationLoader.Load("config.json");
      var app = new Application(factory, configuration, 2);
      app.Start();
    }
  }
}