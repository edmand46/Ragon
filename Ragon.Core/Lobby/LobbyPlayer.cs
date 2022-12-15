using Ragon.Server;

namespace Ragon.Core.Lobby;

public enum LobbyPlayerStatus
{
  Unauthorized,
  Authorized,
}

public class LobbyPlayer
{
  public string Id { get; private set; }
  public string Name { get; set; }
  public byte[] AdditionalData { get; set; }
  public LobbyPlayerStatus Status { get; set; }
  public INetworkConnection Connection { get; private set; }
  
  public LobbyPlayer(INetworkConnection connection)
  {
    Id = Guid.NewGuid().ToString();
    Connection = connection;
    Status = LobbyPlayerStatus.Unauthorized;
    Name = "None";
    AdditionalData = Array.Empty<byte>();
  }
}