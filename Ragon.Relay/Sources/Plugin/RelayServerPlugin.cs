using System;
using Newtonsoft.Json;
using Ragon.Server;
using Ragon.Server.Plugin;

namespace Ragon.Relay;

public class RelayServerPlugin: BaseServerPlugin
{
  public override bool OnCommand(string command, string payload)
  {
    Console.WriteLine(command);
    if (command == "kick-player")
    {
      var commandPayload = JsonConvert.DeserializeObject<KickPlayerCommand>(payload);
      var player = Server.GetPlayerById(commandPayload.Id);
      if (player != null)
        player.Connection.Close();
      else
        Console.WriteLine($"Player not found with Id {commandPayload.Id}");
    }
    
    return true;
  }

  public override IRoomPlugin CreateRoomPlugin(RoomInformation information)
  {
    return new RelayRoomPlugin();
  }
}