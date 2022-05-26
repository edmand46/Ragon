using System;
using System.IO;
using Newtonsoft.Json;
using NLog;
using Logger = NLog.Logger;

namespace Ragon.Core
{
  public static class ConfigurationLoader
  {
    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
    private static readonly string _serverVersion = "1.0.3-rc";

    private static void CopyrightInfo()
    {
      _logger.Info($"Server Version: {_serverVersion}");
      _logger.Info($"Machine Name: {Environment.MachineName}");
      _logger.Info($"OS: {Environment.OSVersion}");
      _logger.Info($"Processors: {Environment.ProcessorCount}");
      _logger.Info($"Runtime Version: {Environment.Version}");
      
      _logger.Info("==================================");
      _logger.Info("=                                =");
      _logger.Info($"={"Ragon".PadBoth(32)}=");
      _logger.Info("=                                =");
      _logger.Info("==================================");
    }

    public static Configuration Load(string filePath)
    {
      CopyrightInfo();
      
      var data = File.ReadAllText(filePath);
      var configuration = JsonConvert.DeserializeObject<Configuration>(data);
      return configuration;
    }
  }
}