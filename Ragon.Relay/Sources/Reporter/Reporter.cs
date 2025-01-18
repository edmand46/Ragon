using Google.Protobuf;
using Google.Protobuf.Collections;
using Ragon.Server;

namespace Ragon.Relay;

public class Reporter
{
  private readonly Client _client;
  private readonly IRagonServer _server;
  private readonly RelayConfiguration _configuration;

  public Reporter(
    RelayConfiguration relayConfiguration,
    IRagonServer server,
    string host,
    int port
  )
  {
    _client = new Client(host, port);
    _server = server;
    _configuration = relayConfiguration;
  }

  public void Done()
  {
    for (var i = 0; i < 10; i++)
    {
      var message = new Data();
      message.Statistics = new Statistics()
      {
        Connections = _server.ConnectionRegistry.Contexts.Count,
        ConnectionsLimit = _configuration.LimitConnections,
        Rooms = _server.Lobby.Rooms.Count,
        RoomsLimit = _configuration.LimitRooms,
      };

      var room = new Room()
      {
        Id = $"Room ID {i}",
      };

      for (var j = 0; j < 10; j++)
      {
        room.Players.Add(new Player()
        {
          Id = $"Player ID {i}",
        });
      }

      message.Room = room;

      _client.Send(message.ToByteArray());
    }
  }
}