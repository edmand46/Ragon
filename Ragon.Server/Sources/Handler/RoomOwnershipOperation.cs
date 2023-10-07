using NLog;
using Ragon.Protocol;
using Ragon.Server.Entity;

namespace Ragon.Server.Handler;

public sealed class RoomOwnershipOperation : BaseOperation
{
  private readonly Logger _logger = LogManager.GetCurrentClassLogger();
  
  public RoomOwnershipOperation(RagonBuffer reader, RagonBuffer writer) : base(reader, writer)
  {
  }

  public override void Handle(RagonContext context, byte[] data)
  {
    
  }
}