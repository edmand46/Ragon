using Ragon.Protocol;

namespace Ragon.Client
{

  internal class PlayerUserDataHandler: IHandler
  {
    private RagonPlayerCache _playerCache;
    private RagonListenerList _listenerList;

    public PlayerUserDataHandler(
      RagonPlayerCache playerCache,
      RagonListenerList listenerList
    )
    {
      _playerCache = playerCache;
      _listenerList = listenerList;
    }
    public void Handle(RagonBuffer reader)
    {
      var playerPeerId = reader.ReadUShort();
      var player = _playerCache.GetPlayerByPeer(playerPeerId);

      if (player != null)
      {
        player.UserData.Read(reader);
        
        _listenerList.OnPlayerUserData();
      }
    }
  }
}