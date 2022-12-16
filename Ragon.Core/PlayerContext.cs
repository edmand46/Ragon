using NLog;
using Ragon.Core.Game;
using Ragon.Core.Lobby;
using Ragon.Core.Time;
using Ragon.Server;

namespace Ragon.Core;

public class PlayerContext: IDisposable
{
  public INetworkConnection Connection { get; }
  public Loop Loop;
  public ILobby Lobby { get; set; }
  public LobbyPlayer LobbyPlayer { private set; get; }
  public Room? Room { get; set; }
  public RoomPlayer? RoomPlayer { get; set; }

  public PlayerContext(INetworkConnection conn, LobbyPlayer player)
  {
    Connection = conn;
    LobbyPlayer = player;
  }

  public void Dispose()
  {
    RoomPlayer?.Room.RemovePlayer(RoomPlayer);
  }
}