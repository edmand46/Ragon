using System;

namespace Ragon.Core
{
  public struct Event
  {
    public EventType Type;
    public DeliveryType Delivery;
    public uint PeerId;
    public byte[] Data;
  }
}