using Ragon.Protocol;
using Ragon.Server.Event;
using Ragon.Server.IO;

namespace Ragon.Server.Handler;

public class RoomEventOperation : BaseOperation
{
  public RoomEventOperation(RagonBuffer reader, RagonBuffer writer) : base(reader, writer)
  {
  }

  public override void Handle(RagonContext context, NetworkChannel channel)
  {
    var room = context.Room;
    var player = context.RoomPlayer;

    var eventId = Reader.ReadUShort();
    var replicationMode = (RagonReplicationMode)Reader.ReadByte();
    var targetMode = (RagonTarget)Reader.ReadByte();
    var targetPlayerPeerId = (ushort)0;
    
    if (targetMode == RagonTarget.Player)
      targetPlayerPeerId = Reader.ReadUShort();

    var @event = new RagonEvent(player, eventId);
    @event.Read(Reader);

    Writer.Clear();
    Writer.WriteUShort(eventId);
    Writer.WriteUShort(player.Connection.Id);
    Writer.WriteUShort((ushort) replicationMode);

    var sendData = Writer.ToArray();
    
    if (targetMode == RagonTarget.Player && room.Players.TryGetValue(targetPlayerPeerId, out var targetPlayer))
    {
      targetPlayer.Connection.Reliable.Send(sendData);
      return;
    }

    foreach (var roomPlayer in room.ReadyPlayersList)
     roomPlayer.Connection.Reliable.Send(sendData); 
  }
}