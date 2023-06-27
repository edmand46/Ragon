using NLog;
using Ragon.Protocol;
using Ragon.Server.Entity;

namespace Ragon.Server.Handler;

public sealed class RoomOwnershipOperation : IRagonOperation
{
  private readonly Logger _logger = LogManager.GetCurrentClassLogger();
  public void Handle(RagonContext context, RagonBuffer reader, RagonBuffer writer)
  {
   
  }
}