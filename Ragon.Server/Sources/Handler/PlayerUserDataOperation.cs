using NLog;
using Ragon.Protocol;
using Ragon.Server.IO;
using Ragon.Server.Lobby;

namespace Ragon.Server.Handler
{
  public class PlayerUserDataOperation : BaseOperation
  {
    private readonly ILogger _logger = LogManager.GetCurrentClassLogger();
    private readonly int _userDataLimit;
    
    public PlayerUserDataOperation(
      RagonBuffer reader,
      RagonBuffer writer,
      int userDataLimit
    ) : base(reader, writer)
    {
      _userDataLimit = userDataLimit;
    }

    public override void Handle(RagonContext context, NetworkChannel channel)
    {
      if (context.ConnectionStatus == ConnectionStatus.Unauthorized)
      {
        _logger.Warn($"Player {context.Connection.Id} not authorized for this request");
        return;
      }

      var playerUserData = Reader.ReadBytes(Reader.Capacity);
      if (playerUserData.Length > _userDataLimit)
      {
        _logger.Warn($"Player {context.Connection.Id} exceeded user data limit");
        return;
      }
      
      context.UserData.Data = playerUserData;
    }
  }
}