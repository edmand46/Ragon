using NLog;
using Ragon.Protocol;
using Ragon.Server.IO;
using Ragon.Server.Lobby;

namespace Ragon.Server.Handler
{

  public class PlayerDataOperation : BaseOperation
  {
    private readonly ILogger _logger = LogManager.GetCurrentClassLogger();

    public PlayerDataOperation(RagonBuffer reader, RagonBuffer writer) : base(reader, writer)
    {
    }

    public override void Handle(RagonContext context, NetworkChannel channel)
    {
      if (context.ConnectionStatus == ConnectionStatus.Unauthorized)
      {
        _logger.Warn($"Player {context.Connection.Id} not authorized for this request");
        return;
      }

      var playerDataLen = Reader.ReadUShort();
      var playerData = Reader.ReadBytes(playerDataLen);
      
      var roomPlayer = context.RoomPlayer;
      if (roomPlayer != null)
      {
        roomPlayer.UserData.Data = playerData;
        return;
      }
      
      var lobbyPlayer = context.RoomPlayer;
      if (lobbyPlayer != null)
      {
        lobbyPlayer.UserData.Data = playerData;
      }
      
    }
  }
}