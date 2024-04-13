namespace Ragon.Client;

public interface IRagonRoomListListener
{
  public void OnRoomListUpdate(IReadOnlyList<RagonRoomInformation> roomsInfos);
}