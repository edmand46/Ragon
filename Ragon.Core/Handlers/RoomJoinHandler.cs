using NLog;
using Ragon.Common;
using Ragon.Core.Game;
using Ragon.Core.Lobby;

namespace Ragon.Core.Handlers;

public sealed class JoinHandler : IHandler
{
  private Logger _logger = LogManager.GetCurrentClassLogger();

  public void Handle(PlayerContext context, RagonSerializer reader, RagonSerializer writer)
  {
    var roomId = reader.ReadString();
    var lobbyPlayer = context.LobbyPlayer;

    if (!context.Lobby.FindRoomById(roomId, out var existsRoom))
    {
      JoinFailed(lobbyPlayer, writer);
      
      _logger.Trace($"Player {context.Connection.Id}|{context.LobbyPlayer.Name} failed to join room {roomId}");
      return;
    }

    var roomPlayer = new RoomPlayer(lobbyPlayer.Connection, lobbyPlayer.Id, lobbyPlayer.Name);

    context.Room?.RemovePlayer(context.RoomPlayer);
    context.Room = existsRoom;
    context.RoomPlayer = roomPlayer;

    existsRoom.AddPlayer(roomPlayer);

    JoinSuccess(roomPlayer, existsRoom, writer);
    
    _logger.Trace($"Player {context.Connection.Id}|{context.LobbyPlayer.Name} joined to {existsRoom.Id}");
  }

  private void JoinSuccess(RoomPlayer player, Room room, RagonSerializer writer)
  {
    writer.Clear();
    writer.WriteOperation(RagonOperation.JOIN_SUCCESS);
    writer.WriteString(room.Id);
    writer.WriteString(player.Id);
    writer.WriteString(room.Owner.Id);
    writer.WriteUShort((ushort) room.Info.Min);
    writer.WriteUShort((ushort) room.Info.Max);
    writer.WriteString(room.Info.Map);

    var sendData = writer.ToArray();
    player.Connection.Reliable.Send(sendData);
  }

  private void JoinFailed(LobbyPlayer player, RagonSerializer writer)
  {
    writer.Clear();
    writer.WriteOperation(RagonOperation.JOIN_FAILED);
    writer.WriteString($"Room not exists");

    var sendData = writer.ToArray();
    player.Connection.Reliable.Send(sendData);
  }
}