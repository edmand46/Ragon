using Ragon.Protocol;
using Ragon.Server.IO;
using Ragon.Server.Lobby;
using Ragon.Server.Logging;

namespace Ragon.Server.Handler
{
  public class PlayerUserDataOperation : BaseOperation
  {
    private readonly IRagonLogger _logger = LoggerManager.GetLogger(nameof(PlayerUserDataOperation));
    private readonly int _userDataLimit;

    public PlayerUserDataOperation(
      RagonStream reader,
      RagonStream writer,
      int userDataLimit
    ) : base(reader, writer)
    {
      _userDataLimit = userDataLimit;
    }

    public override void Handle(RagonContext context, NetworkChannel channel)
    {
      if (context.ConnectionStatus == ConnectionStatus.Unauthorized)
      {
        _logger.Warning($"Player {context.Connection.Id} not authorized for this request");
        return;
      }

      context.UserData.Read(Reader);
    }
  }
}