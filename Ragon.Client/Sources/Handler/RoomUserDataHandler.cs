using Ragon.Protocol;

namespace Ragon.Client
{
  internal class RoomUserDataHandler: IHandler
  {
    private readonly RagonClient _client;
    private readonly RagonListenerList _listenerList;
    public RoomUserDataHandler(RagonClient client, RagonListenerList listenerList)
    {
      _client = client;
      _listenerList = listenerList;
    }
    
    public void Handle(RagonBuffer reader)
    {
      _client.Room?.HandleProperties(reader);
      
      _listenerList.OnRoomUserData();
    }
  }
}