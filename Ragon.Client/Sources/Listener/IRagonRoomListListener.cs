namespace Ragon.Client;

public interface IRagonRoomListListener
{
  public void OnRoomListUpdate(RagonClient client, IReadOnlyList<RagonRoomInformation> roomsInfos);
}