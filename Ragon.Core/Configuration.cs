using Newtonsoft.Json;
using NLog;

namespace Ragon.Core;

[Serializable]
public struct Configuration
{
  public string Key;
  public string Protocol;
  public string Socket;
  public ushort SendRate;
  public ushort Port;
  public int SkipTimeout;
  public int ReconnectTimeout;
  public int LimitConnections;
  public int LimitPlayersPerRoom;
  public int LimitRooms;

  private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
  private static readonly string ServerVersion = "1.1.0-rc";

  private static void CopyrightInfo()
  {
    Logger.Info($"Server Version: {ServerVersion}");
    Logger.Info($"Machine Name: {Environment.MachineName}");
    Logger.Info($"OS: {Environment.OSVersion}");
    Logger.Info($"Processors: {Environment.ProcessorCount}");
    Logger.Info($"Runtime Version: {Environment.Version}");
    Logger.Info("==================================");
    Logger.Info("|                                |");
    Logger.Info("|            Ragon               |");
    Logger.Info("|                                |");
    Logger.Info("==================================");
  }

  public static Configuration Load(string filePath)
  {
    CopyrightInfo();
      
    var data = File.ReadAllText(filePath);
    var configuration = JsonConvert.DeserializeObject<Configuration>(data);
    return configuration;
  }
}