using System;
using System.Threading.Tasks;

namespace Ragon.Core;

public interface  IAuthorizationProvider
{
  Task OnAuthorizationRequest(string key, string playerName, byte[] additionalData, Action<string, string> Accept, Action<uint> Reject);
}