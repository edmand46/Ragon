using System;

namespace Ragon.Core
{
  public struct Event
  {
    public EventType Type;
    public DeliveryType Delivery;
    public byte[] Data;
    public uint PeerId;
  }
}