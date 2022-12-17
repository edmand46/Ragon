using Ragon.Common;

namespace Ragon.Core.Game;

public class EntityEvent
{
  public RoomPlayer Invoker { get; private set; }
  public ushort EventId { get; private set; }
  public byte[] EventData { get; private set; }
  public RagonTarget Target { set; private get; }

  public EntityEvent(
    RoomPlayer invoker,
    ushort eventId,
    byte[] payload,
    RagonTarget target
  )
  {
    Invoker = invoker;
    EventId = eventId;
    EventData = payload;
    Target = target;
  }
}