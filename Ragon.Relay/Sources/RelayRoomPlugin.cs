using System;
using System.Collections.Generic;
using System.Diagnostics;
using NLog;
using Ragon.Server.Entity;
using Ragon.Server.IO;
using Ragon.Server.Plugin;
using Ragon.Server.Room;

namespace Ragon.Relay;

public class RelayRoomPlugin : BaseRoomPlugin
{
  private Logger _logger = LogManager.GetCurrentClassLogger();

  public override bool OnPlayerJoined(RagonRoomPlayer player)
  {
    // _logger.Trace($"Player {player.Name}|{player.Connection.Id} joined");
    return false;
  }

  public override bool OnPlayerLeaved(RagonRoomPlayer player)
  {
    // _logger.Trace($"Player {player.Name}|{player.Connection.Id} leaved");
    return false;
  }

  public override bool OnData(RagonRoomPlayer player, byte[] data)
  {
    // _logger.Trace($"Data received from {player.Name}|{player.Connection.Id}");

    // All Players
    // Room.ReplicateData(new Byte[] { 30, 40, 50 }, NetworkChannel.RELIABLE);
    // Selected Player
    // Room.ReplicateData(new byte[] { 10, 30, 40 }, new List<RagonRoomPlayer> { player }, NetworkChannel.RELIABLE);
    
    return true;
  }
}