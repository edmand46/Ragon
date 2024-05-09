namespace Ragon.Client;

public interface IRagonRoomUserDataListener
{
  public void OnUserDataUpdated(RagonClient client, IReadOnlyList<string> changes);
}