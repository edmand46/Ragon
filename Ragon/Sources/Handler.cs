using System;
using System.Threading.Tasks;

namespace Ragon.Core;

public interface IApplicationHandler
{
  Task OnAuthorizationRequest(string key, string playerName, byte[] additionalData, Action<string, string> Accept, Action<uint> Reject);
  public void OnCustomEvent(ushort peerId, ReadOnlySpan<byte> payload);
  public void OnJoin(ushort peerId);
  public void OnLeave(ushort peerId);
}