using System;
using System.Text;
using Ragon.Core;

namespace Game.Source;

public class GameAuthorizer: AuthorizationManager
{
  private Configuration _configuration;
  public GameAuthorizer(Configuration configuration)
  {
    _configuration = configuration;
  }

  public override bool OnAuthorize(uint peerId, ref ReadOnlySpan<byte> payload)
  {
    var apiKey = Encoding.UTF8.GetString(payload);
    return _configuration.ApiKey == apiKey;
  }
}