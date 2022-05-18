using System;
using System.Diagnostics;
using System.IO;
using NLog;

namespace Ragon.Core
{
  public class Bootstrap
  {
    private ILogger _logger = LogManager.GetCurrentClassLogger();

    public void Configure(PluginFactory factory)
    {
      _logger.Info("Configure application...");
      var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
      var configuration = ConfigurationLoader.Load(filePath);
      var app = new Application(factory, configuration, 2);
      app.Start();
    }
  }
}