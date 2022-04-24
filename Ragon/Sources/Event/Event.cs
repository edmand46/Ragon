namespace Ragon.Core
{
  public struct Event
  {
    public EventType Type;
    public uint PeerId;
    public byte[] Data;
  }
}