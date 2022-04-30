using System;

namespace Ragon.Core;

public class AuthorizationManager
{
  public virtual bool OnAuthorize(uint peerId, ref ReadOnlySpan<byte> payload)
  {
    return true;
  }
}