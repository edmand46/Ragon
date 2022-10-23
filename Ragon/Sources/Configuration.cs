using System;
using System.IO;
using Newtonsoft.Json;
using NLog;

namespace Ragon.Core
{
  [Serializable]
  public struct Configuration
  {
    public string Key;
    public string Protocol;
    public int StatisticsInterval;
    public ushort SendRate;
    public ushort Port;
    public int SkipTimeout;
    public int ReconnectTimeout;
    public int MaxConnections;
    public int MaxPlayersPerRoom;
    public int MaxRooms;
    
    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
    private static readonly string _serverVersion = "1.0.22-rc";

    private static void CopyrightInfo()
    {
      _logger.Info($"Server Version: {_serverVersion}");
      _logger.Info($"Machine Name: {Environment.MachineName}");
      _logger.Info($"OS: {Environment.OSVersion}");
      _logger.Info($"Processors: {Environment.ProcessorCount}");
      _logger.Info($"Runtime Version: {Environment.Version}");
      _logger.Info("==================================");
      _logger.Info("|                                |");
      _logger.Info("|            Ragon               |");
      _logger.Info("|                                |");
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