using NLog;
using Ragon.Common;
using Ragon.Core.Lobby;

namespace Ragon.Core.Handlers;

public sealed class AuthHandler: IHandler
{
  private Logger _logger = LogManager.GetCurrentClassLogger();
  
  public void Handle(PlayerContext context, RagonSerializer reader, RagonSerializer writer)
  {
    if (context.LobbyPlayer.Status == LobbyPlayerStatus.Authorized)
    {
      _logger.Warn("Player already authorized");    
      return;
    }
    
    var key = reader.ReadString();
    var playerName = reader.ReadString();
    var additionalData = reader.ReadData(reader.Size);

    context.LobbyPlayer.Name = playerName;
    context.LobbyPlayer.AdditionalData = additionalData.ToArray();
    context.LobbyPlayer.Status = LobbyPlayerStatus.Authorized;

    var playerId = context.LobbyPlayer.Id;
    
    writer.Clear();
    writer.WriteOperation(RagonOperation.AUTHORIZED_SUCCESS);
    writer.WriteString(playerId);
    writer.WriteString(playerName);
    
    var sendData = writer.ToArray();
    context.Connection.ReliableChannel.Send(sendData);
    
    _logger.Trace($"Player {context.Connection.Id}|{context.LobbyPlayer.Name} authorized");
  }
}