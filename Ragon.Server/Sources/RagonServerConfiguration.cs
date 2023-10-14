/*
 * Copyright 2023 Eduard Kargin <kargin.eduard@gmail.com>
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using Newtonsoft.Json;
using NLog;

namespace Ragon.Server;

public enum ServerType
{
  ENET,
  WEBSOCKET,
}

[Serializable]
public struct RagonServerConfiguration
{
  public string ServerKey;
  public string ServerType;
  public ushort ServerTickRate;
  public string GameProtocol;
  public ushort Port;
  public ushort HttpPort;
  public string HttpKey;
  public int LimitConnections;
  public int LimitPlayersPerRoom;
  public int LimitRooms;
  public int LimitBufferedEvents;
  public Dictionary<string, string> WebHooks;

  private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
  private static readonly string ServerVersion = "1.3.1";
  private static Dictionary<string, ServerType> _serverTypes = new Dictionary<string, ServerType>()
  {
    {"enet", Server.ServerType.ENET},
    {"websocket", Server.ServerType.WEBSOCKET}
  };

  public static RagonServerConfiguration Load(string filePath)
  {
    CopyrightInfo();
      
    var data = File.ReadAllText(filePath);
    var configuration = JsonConvert.DeserializeObject<RagonServerConfiguration>(data);
    return configuration;
  }

  private static void CopyrightInfo()
  {
    Logger.Info($"Server Version: {ServerVersion}");
    Logger.Info($"Machine Name: {Environment.MachineName}");
    Logger.Info($"OS: {Environment.OSVersion}");
    Logger.Info($"Processors: {Environment.ProcessorCount}");
    Logger.Info($"Runtime Version: {Environment.Version}");
    Logger.Info("==================================");
    Logger.Info(@"   ___    _   ___  ___  _  _ ");
    Logger.Info(@"  | _ \  /_\ / __|/ _ \| \| |");
    Logger.Info(@"  |   / / _ \ (_ | (_) | .` |");
    Logger.Info(@"  |_|_\/_/ \_\___|\___/|_|\_|");
    Logger.Info("==================================");
  }

  public static ServerType GetServerType(string type) => _serverTypes[type];
}