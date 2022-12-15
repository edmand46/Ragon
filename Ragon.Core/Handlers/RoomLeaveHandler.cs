using NLog;
using Ragon.Common;

namespace Ragon.Core.Handlers;

public sealed class LeaveHandler: IHandler
{
  private Logger _logger = LogManager.GetCurrentClassLogger();
  public void Handle(PlayerContext context, RagonSerializer reader, RagonSerializer writer)
  {
    var room = context.Room;
    var roomPlayer = context.RoomPlayer;
    if (room != null)
    { 
      context.Room?.RemovePlayer(roomPlayer);
      _logger.Trace($"Player {context.Connection.Id}|{context.LobbyPlayer.Name} leaved from {room.Id}");
    }
  }
}