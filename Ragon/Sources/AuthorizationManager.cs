namespace Ragon.Core;

public class AuthorizationManager
{
  public virtual bool OnAuthorize(uint peerId, byte[] payload)
  {
    return true;
  }
}