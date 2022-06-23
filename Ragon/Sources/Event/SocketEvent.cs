using System;

namespace Ragon.Core
{
  public struct SocketEvent
  {
    public EventType Type;
    public DeliveryType Delivery;
    public byte[] Data;
    public uint PeerId;
  }
}