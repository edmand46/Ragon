using Ragon.Core;

namespace Game.Source;

public class GameAuthorizer: AuthorizationManager
{
  public override bool OnAuthorize(uint peerId, byte[] payload)
  {
    return true;
  }
}