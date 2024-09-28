using NUnit.Mocks;
using Ragon.Client;
using Ragon.Relay;
using Ragon.Server;
using Ragon.Server.Logging;
using Ragon.Server.Plugin;

namespace Ragon.Tests;

public class Tests
{
  private RagonClient _client;
  private RagonServer _server;
  
  [SetUp]
  public void Setup()
  {
    LoggerManager.SetLoggerFactory(new RelayLoggerFactory());
    
    var fakeNetwork = new FakeNetwork();
    var serverConfiguration = new RagonServerConfiguration()
    {
      LimitConnections = 100,
      LimitRooms = 10,
      LimitBufferedEvents = 500,
      LimitPlayersPerRoom = 10,
      LimitUserDataSize = 512,
      LimitPropertySize = 512,
      Port = 5000,
      Protocol = "udp",
      ServerKey = "defaultkey",
      ServerTickRate = 30,
      ServerAddress = "0.0.0.0",
    };
    
    _client = new RagonClient(fakeNetwork.ClientNetwork, 30);
    _server = new RagonServer(fakeNetwork.ServerNetwork, new BaseServerPlugin(), serverConfiguration);
  }

  [Test]
  public void Test1()
  {
   
  }
}