using NLog;
using Ragon.Common;
using Ragon.Core.Game;
using Ragon.Core.Lobby;

namespace Ragon.Core.Handlers;

public sealed class CreateHandler: IHandler
{
  private RagonRoomParameters _roomParameters = new();
  private Logger _logger = LogManager.GetCurrentClassLogger();
  
  public void Handle(PlayerContext context, RagonSerializer reader, RagonSerializer writer)
  {
    if (context.LobbyPlayer.Status == LobbyPlayerStatus.Unauthorized)
    {
      _logger.Warn($"Player {context.Connection.Id} not authorized for this request");
      return;
    }

    var custom = reader.ReadBool();
    var roomId = Guid.NewGuid().ToString();
    
    if (custom)
    {
      roomId = reader.ReadString();
      if (context.Lobby.FindRoomById(roomId, out _))
      { 
        writer.Clear();
        writer.WriteOperation(RagonOperation.JOIN_FAILED);
        writer.WriteString($"Room with id {roomId} already exists");
            
        var sendData = writer.ToArray();
        context.Connection.ReliableChannel.Send(sendData);
        
        _logger.Trace($"Player {context.Connection.Id}|{context.LobbyPlayer.Name} join failed to room {roomId}, room already exist");
        return;        
      }
    }
    
    _roomParameters.Deserialize(reader);
    
    var information = new RoomInformation()
    {
        Map = _roomParameters.Map,
        Max = _roomParameters.Max,
        Min = _roomParameters.Min,
    };

    var lobbyPlayer = context.LobbyPlayer;
    var roomPlayer = new RoomPlayer(lobbyPlayer.Connection, lobbyPlayer.Id, lobbyPlayer.Name);
    
    var room = new Room(roomId, information, new PluginBase());
    room.AddPlayer(roomPlayer);

    context.Room?.RemovePlayer(context.RoomPlayer);
    context.Room = room;
    context.RoomPlayer = roomPlayer;
    context.Lobby.Persist(room);
    
    _logger.Trace($"Player {context.Connection.Id}|{context.LobbyPlayer.Name} create room {room.Id} {information}");
    
    JoinSuccess(roomPlayer, room, writer);
    
    _logger.Trace($"Player {context.Connection.Id}|{context.LobbyPlayer.Name} joined to room {room.Id}");
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
  }
}