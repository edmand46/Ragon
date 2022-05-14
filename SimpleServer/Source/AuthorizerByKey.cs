using System;
using System.Text;
using Ragon.Core;

namespace Game.Source;

public class AuthorizerByKey: AuthorizationManager
{
  private Configuration _configuration;
  public AuthorizerByKey(Configuration configuration)
  {
    _configuration = configuration;
  }

  public override bool OnAuthorize(uint peerId, ref ReadOnlySpan<byte> payload)
  {
    var key = Encoding.UTF8.GetString(payload);
    return _configuration.Key == key;
  }
}