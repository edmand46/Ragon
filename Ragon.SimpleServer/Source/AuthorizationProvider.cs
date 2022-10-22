using System;
using System.Threading.Tasks;
using Ragon.Core;

namespace Game.Source;

public class ApplicationHandlerByKey: IApplicationHandler
{
  private Configuration _configuration;
  public ApplicationHandlerByKey(Configuration configuration)
  {
    _configuration = configuration;
  }
  
  public async Task OnAuthorizationRequest(string key, string name, byte[] additionalData, Action<string, string> accept, Action<uint> reject)
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

  public void OnCustomEvent(ushort peerId, ReadOnlySpan<byte> payload)
  {
    
  }

  public void OnJoin(ushort peerId)
  {
    
  }

  public void OnLeave(ushort peerId)
  {
    
  }
}