using NLog;
using Ragon.Protocol;
using Ragon.Server.IO;
using Ragon.Server.Lobby;

namespace Ragon.Server.Handler
{
  public class PlayerPropertiesOperation : BaseOperation
  {
    private readonly ILogger _logger = LogManager.GetCurrentClassLogger();

    public PlayerPropertiesOperation(RagonBuffer reader, RagonBuffer writer) : base(reader, writer)
    {
    }

    public override void Handle(RagonContext context, NetworkChannel channel)
    {
      if (context.ConnectionStatus == ConnectionStatus.Unauthorized)
      {
        _logger.Warn($"Player {context.Connection.Id} not authorized for this request");
        return;
      }
      
      var playerData = Reader.ReadBytes(Reader.Capacity);
      context.UserData.Data = playerData;
    }
  }
}