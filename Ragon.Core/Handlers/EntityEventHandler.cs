using NLog;
using Ragon.Common;

namespace Ragon.Core.Handlers;

public sealed class EntityEventHandler: IHandler
{
  private Logger _logger = LogManager.GetCurrentClassLogger();
  
  public void Handle(PlayerContext context, RagonSerializer reader, RagonSerializer writer)
  {
    var player = context.RoomPlayer;
    var room = context.Room;
    var entityId = reader.ReadUShort();
    
    if (!room.Entities.TryGetValue(entityId, out var ent))
    {
      _logger.Warn($"Entity not found for event with Id {entityId}");
      return;
    }
    
    var eventId = reader.ReadUShort();
    var eventMode = (RagonReplicationMode) reader.ReadByte();
    var targetMode = (RagonTarget) reader.ReadByte();
    var payloadData = reader.ReadData(reader.Size);
    var targetPlayerPeerId = reader.ReadUShort();
    
    if (targetMode == RagonTarget.Player && context.Room.Players.TryGetValue(targetPlayerPeerId, out var targetPlayer)) 
    {
      Span<byte> payloadRaw = stackalloc byte[payloadData.Length];
      ReadOnlySpan<byte> payload = payloadRaw;
      payloadData.CopyTo(payloadRaw);

      _logger.Trace($"Event {eventId} Payload: {payloadData.Length} to {targetMode}");
      ent.ReplicateEvent(player, eventId, payload, eventMode, targetPlayer);
    }
    else
    {
      Span<byte> payloadRaw = stackalloc byte[payloadData.Length];
      ReadOnlySpan<byte> payload = payloadRaw;
      payloadData.CopyTo(payloadRaw);
      
      _logger.Trace($"Event {eventId} Payload: {payloadData.Length} to {targetMode}");
      ent.ReplicateEvent(player, eventId, payload, eventMode, targetMode);
    }
  }
}