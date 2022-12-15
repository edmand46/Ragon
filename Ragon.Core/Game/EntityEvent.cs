using Ragon.Common;

namespace Ragon.Core.Game;

public class EntityEvent
{
  public ushort PeerId { get; set; }
  public ushort EventId { get; set; }
  public byte[] EventData { get; set; }
  public RagonTarget Target { set; get; }
}