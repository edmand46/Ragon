using NLog;
using Ragon.Common;
using Ragon.Core.Game;
using Ragon.Core.Lobby;

namespace Ragon.Core.Handlers;

public sealed class JoinOrCreateHandler : IHandler
{
  private RagonRoomParameters _roomParameters = new();
  private Logger _logger = LogManager.GetCurrentClassLogger();
  
  public void Handle(PlayerContext context, RagonSerializer reader, RagonSerializer writer)
  {
    if (context.LobbyPlayer.Status == LobbyPlayerStatus.Unauthorized)
    {
      _logger.Warn("Player not authorized for this request");
      return;
    }

    var roomId = Guid.NewGuid().ToString();
    var lobbyPlayer = context.LobbyPlayer;
    
    _roomParameters.Deserialize(reader);

    if (context.Lobby.FindRoomByMap(_roomParameters.Map, out var existsRoom))
    {
      var roomPlayer = new RoomPlayer(lobbyPlayer.Connection, lobbyPlayer.Id, lobbyPlayer.Name);
      
      context.Room?.RemovePlayer(context.RoomPlayer);
      context.Room = existsRoom;
      context.RoomPlayer = roomPlayer;
      
      existsRoom.AddPlayer(roomPlayer);
      
      JoinSuccess(roomPlayer, existsRoom, writer);
    }
    else
    {
      var information = new RoomInformation()
      {
        Map = _roomParameters.Map,
        Max = _roomParameters.Max,
        Min = _roomParameters.Min,
      };

      var room = new Room(roomId, information);
      context.Lobby.Persist(room);
      
      var roomPlayer = new RoomPlayer(lobbyPlayer.Connection, lobbyPlayer.Id, lobbyPlayer.Name);
      room.AddPlayer(roomPlayer);
      
      context.Room?.RemovePlayer(context.RoomPlayer);
      context.Room = room;
      context.RoomPlayer = roomPlayer;
      
      _logger.Trace($"Player {context.Connection.Id}|{context.LobbyPlayer.Name} create room {room.Id} {information}");

      JoinSuccess(roomPlayer, room, writer);
      
      context.Loop.Run(room);
    }
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
    player.Connection.ReliableChannel.Send(sendData);
    
    _logger.Trace($"Joined to room {room.Id}");
  }
}