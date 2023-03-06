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

using NLog;
using Ragon.Client;
using Ragon.Protocol;
using Xunit.Abstractions;

namespace Ragon.Core.Tests;

public class Connection
{
  private readonly ITestOutputHelper _testOutputHelper;

  public Connection(ITestOutputHelper testOutputHelper)
  {
    _testOutputHelper = testOutputHelper;
  }

  public class Game : IRagonListener
  {
    private RagonClient _client;

    public Game(RagonClient client, TaskCompletionSource taskCompletionSource)
    {
      _client = client;
    }

    public void OnConnected(RagonClient client)
    {
      Console.WriteLine("Console Connected");
      RagonLog.Trace("Connected");

      client.Session.AuthorizeWithKey("defaultkey", "Player Eduard", Array.Empty<byte>());
    }

    public void OnAuthorizationSuccess(RagonClient client, string playerId, string playerName)
    {
      RagonLog.Trace("Authorized");

      client.Session.Create("Example", 1, 20);
    }

    public void OnAuthorizationFailed(RagonClient client, string message)
    {
      RagonLog.Trace($"Authorization failed: {message}");
    }

    public void OnJoined(RagonClient client)
    {
    }

    public void OnFailed(RagonClient client, string message)
    {
      RagonLog.Trace("Failed to join");
    }

    public void OnLeft(RagonClient client)
    {
      RagonLog.Trace("Left");
    }

    public void OnDisconnected(RagonClient client)
    {
      RagonLog.Trace("Disconnected");
    }

    public void OnPlayerJoined(RagonClient client, RagonPlayer player)
    {
      RagonLog.Trace("Player joined");
    }

    public void OnPlayerLeft(RagonClient client, RagonPlayer player)
    {
      RagonLog.Trace("Player left");
    }

    public void OnOwnershipChanged(RagonClient client, RagonPlayer player)
    {
      RagonLog.Trace("Owner ship changed");
    }

    public void OnLevel(RagonClient client, string sceneName)
    {
      RagonLog.Trace($"New level: {sceneName}");
      client.Room.SceneLoaded();
    }
  }

  // [Fact]
  // public async void Connect()
  // {
  //   RagonLog.Set(new RagonXUnitLogger(_testOutputHelper));
  //
  //   var joining = new TaskCompletionSource();
  //   var network = new RagonNetwork();
  //   var game = new Game(network, joining);
  //   
  //   network.AddListener(game);
  //   
  //   var clientConfiguration = new RagonConnectionConfiguration();
  //   clientConfiguration.Type = RagonConnectionType.UDP;
  //   clientConfiguration.Protocol = "1.0.0";
  //   clientConfiguration.Address = "127.0.0.1";
  //   clientConfiguration.Port = 5000;
  //
  //   network.Connect(clientConfiguration);
  //   
  //   var relayConfiguration = new Configuration()
  //   {
  //     GameProtocol = "1.0.0",
  //     LimitConnections = 4095,
  //     LimitRooms = 20,
  //     LimitPlayersPerRoom = 20,
  //     Port = 5000,
  //     ServerKey = "defaultkey",
  //     ServerTickRate = 30,
  //     ServerType = "enet"
  //   };
  //   
  //   var relay = new RagonServer(relayConfiguration);
  //   relay.Start(true);
  //
  //   var ticks = 0;
  //   while (true)
  //   {
  //     ticks += 1;
  //     network.Update();
  //     
  //     if (ticks > 100)
  //       break;
  //     
  //     await Task.Delay(100);
  //   }
  //
  //   Assert.Equal(network.Status, RagonStatus.ROOM);
  //   
  //   network.Dispose();
  //   relay.Dispose();
  // }
}