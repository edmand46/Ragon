﻿/*
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

using NLog;
using Ragon.Server;
using Ragon.Server.ENet;
using Ragon.Server.DotNetWebsockets;


namespace Ragon.Relay;

public class Relay
{
  public void Start()
  {
    var logger = LogManager.GetLogger("Ragon.Relay");
    logger.Info("Relay Application");

    var configuration = Configuration.Load("relay.config.json");
    var serverType = Configuration.GetServerType(configuration.ServerType);

    INetworkServer server = null;
    switch (serverType)
    {
      case ServerType.ENET:
        server = new ENetServer();
        break;
      case ServerType.WEBSOCKET:
        server = new DotNetWebSocketServer();
        break;
      default:
        server = new ENetServer();
        break;
    }

    var relay = new RagonServer(server, configuration);
    logger.Info("Started");
    relay.Start();
  }
}