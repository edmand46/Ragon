using Ragon.Protocol;

namespace Ragon.Server.Handler;

public class TimestampSyncOperation: BaseOperation
{
  public TimestampSyncOperation(RagonBuffer reader, RagonBuffer writer) : base(reader, writer)
  {
  }

  public override void Handle(RagonContext context)
  {
    var timestamp0 = Reader.Read(32);
    var timestamp1 = Reader.Read(32);
    var value = new DoubleToUInt() { Int0 = timestamp0, Int1 = timestamp1 };
    
    context.RoomPlayer?.SetTimestamp(value.Double);
  }
}