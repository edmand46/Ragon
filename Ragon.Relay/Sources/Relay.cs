/*
 * Copyright 2023-2024 Eduard Kargin <kargin.eduard@gmail.com>
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

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Newtonsoft.Json;
using Ragon.Server;
using Ragon.Server.ENetServer;
using Ragon.Server.IO;
using Ragon.Server.Logging;
using Ragon.Server.Plugin;
using Ragon.Server.WebSocketServer;

namespace Ragon.Relay
{
  public class Relay
  {
    public void Start()
    {
      LoggerManager.SetLoggerFactory(new RelayLoggerFactory());

      var logger = LoggerManager.GetLogger("Relay");
      logger.Info("Relay Application");

      var data = File.ReadAllText("relay.config.json");
      var configuration = JsonConvert.DeserializeObject<RelayConfiguration>(data);
      var serverType = RagonServerConfiguration.GetServerType(configuration.ServerType);

      INetworkServer networkServer;
      IServerPlugin plugin = new RelayServerPlugin();

      switch (serverType)
      {
        case ServerType.ENET:
          networkServer = new ENetServer();
          break;
        case ServerType.WEBSOCKET:
          networkServer = new WebSocketServer();
          break;
        default:
          networkServer = new ENetServer();
          break;
      }

      var serverConfiguration = new RagonServerConfiguration()
      {
        LimitConnections = configuration.LimitConnections,
        LimitRooms = configuration.LimitRooms,
        LimitBufferedEvents = configuration.LimitBufferedEvents,
        LimitPlayersPerRoom = configuration.LimitPlayersPerRoom,
        LimitUserDataSize = configuration.LimitUserDataSize,
        LimitPropertySize = configuration.LimitPropertySize,
        Port = configuration.Port,
        Protocol = configuration.Protocol,
        ServerKey = configuration.ServerKey,
        ServerTickRate = configuration.ServerTickRate,
        ServerAddress = configuration.ServerAddress,
      };

      var relay = new RagonServer(networkServer, plugin, serverConfiguration);
      relay.Start();

      var sw = Stopwatch.StartNew();
      var tickRateMs = 1000.0 / configuration.ServerTickRate;
      var nextTickMs = tickRateMs;

      while (relay.IsRunning)
      {
        relay.Tick();

        var sleepTime = nextTickMs - sw.Elapsed.TotalMilliseconds;
        if (sleepTime > 0)
        {
          Thread.Sleep((int)sleepTime);
        }

        nextTickMs += tickRateMs;

        if (nextTickMs < sw.Elapsed.TotalMilliseconds)
        {
          nextTickMs = sw.Elapsed.TotalMilliseconds + tickRateMs;
        }
      }

      relay.Dispose();
    }
  }
}