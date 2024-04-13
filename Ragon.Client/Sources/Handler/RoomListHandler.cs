using Ragon.Protocol;

namespace Ragon.Client;

internal class RoomListHandler: IHandler
{
  private RagonListenerList _listenerList;
  private RagonSession _session;
  
  public RoomListHandler(RagonSession session, RagonListenerList list)
  {
    _session = session;
    _listenerList = list;
  }
  
  public void Handle(RagonBuffer reader)
  {
    var roomCount = reader.ReadUShort();
    var roomList = new RagonRoomInformation[roomCount];
    for (int i = 0; i < roomCount; i++)
    {
      var id = reader.ReadString();
      var scene = reader.ReadString();
      var maxPlayers = reader.ReadUShort();
      var minPlayers = reader.ReadUShort();
      var players = reader.ReadUShort();

      var roomInfo = new RagonRoomInformation()
      {
        Id = id,
        Scene = scene,
        PlayerCount = players,
        PlayerMax = maxPlayers,
        PlayerMin = minPlayers,
        Properties = new Dictionary<string, byte[]>() 
      };
      
      roomList[i] = roomInfo;
    }
    
    _listenerList.OnRoomList(roomList);
  }
}