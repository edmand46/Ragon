using Ragon.Protocol;
using Ragon.Server.Event;
using Ragon.Server.IO;
using Ragon.Server.Lobby;
using Ragon.Server.Logging;

namespace Ragon.Server.Handler;

public class RoomEventOperation : BaseOperation
{
  private readonly IRagonLogger _logger = LoggerManager.GetLogger(nameof(RoomEventOperation));
  
  public RoomEventOperation(RagonBuffer reader, RagonBuffer writer) : base(reader, writer)
  {
  }

  public override void Handle(RagonContext context, NetworkChannel channel)
  {
    if (context.ConnectionStatus == ConnectionStatus.Unauthorized)
    {
      _logger.Warning($"Player {context.Connection.Id} not authorized for this request");
      return;
    }

    var room = context.Room;
    var player = context.RoomPlayer;

    var eventId = Reader.ReadUShort();
    var eventMode = (RagonReplicationMode)Reader.ReadByte();
    var targetMode = (RagonTarget)Reader.ReadByte();
    var targetPlayerPeerId = (ushort)0;

    if (targetMode == RagonTarget.Player)
      targetPlayerPeerId = Reader.ReadUShort();

    var @event = new RagonEvent(player, eventId);
    @event.Read(Reader);

    if (targetMode == RagonTarget.Player && room.Players.TryGetValue(targetPlayerPeerId, out var targetPlayer))
    {
      room.ReplicateEvent(player, @event, eventMode, targetPlayer);
      return;
    }

    room.ReplicateEvent(player, @event, eventMode, targetMode);
  }
}