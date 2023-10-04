using Ragon.Protocol;

namespace Ragon.Server.Handler;

public class TimestampSyncOperation: IRagonOperation
{
  public void Handle(RagonContext context, RagonBuffer reader, RagonBuffer writer)
  {
    var timestamp0 = reader.Read(32);
    var timestamp1 = reader.Read(32);
    var value = new DoubleToUInt() { Int0 = timestamp0, Int1 = timestamp1 };
    
    context.RoomPlayer?.SetTimestamp(value.Double);
  }
}