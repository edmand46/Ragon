using Ragon.Server.IO;

namespace Ragon.Server.Room;

public interface IRagonRoom
{
  RagonRoomPlayer GetPlayerByConnection(INetworkConnection connection);
  RagonRoomPlayer GetPlayerById(string id);
}