using Ragon.Server.IO;

namespace Ragon.Server;

public interface IRagonServer
{
  RagonContext? ResolveContext(INetworkConnection connection);
  RagonContext? ResolveContext(string id);
}