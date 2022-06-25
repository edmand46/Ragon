using System;
using System.Threading.Tasks;
using Ragon.Core;

namespace Game.Source;

public class AuthorizationProviderByKey: IAuthorizationProvider
{
  private Configuration _configuration;
  public AuthorizationProviderByKey(Configuration configuration)
  {
    _configuration = configuration;
  }
  
  public async Task OnAuthorizationRequest(string key, string name, byte protocol, byte[] additionalData, Action<string, string> accept, Action<uint> reject)
  {
    if (key == _configuration.Key)
    {
      var playerId = Guid.NewGuid().ToString();
      var playerName = name;
      
      accept(playerId, playerName);
    }
    else
    {
      reject(0);
    }  
  }
}