﻿using System;
using System.Diagnostics;
using System.IO;
using NLog;

namespace Ragon.Core
{
  public class Bootstrap
  {
    private ILogger _logger = LogManager.GetCurrentClassLogger();

    public Application Configure(PluginFactory factory)
    {
      _logger.Info("Configure application...");
      var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
      var configuration = Configuration.Load(filePath);
      var app = new Application(factory, configuration);
      return app;
    }

  }
}