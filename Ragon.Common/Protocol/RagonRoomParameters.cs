using NetStack.Serialization;

namespace Ragon.Common
{
  public class RagonRoomParameters: IRagonSerializable
  {
    public string Map { get; set; }
    public int Min { get; set; } 
    public int Max { get; set; } 
    
    public void Serialize(BitBuffer buffer)
    {
      buffer.AddString(Map);
      buffer.AddInt(Min);
      buffer.AddInt(Max);
    }

    public void Deserialize(BitBuffer buffer)
    {
      Map = buffer.ReadString();
      Min = buffer.ReadInt();
      Max = buffer.ReadInt();
    }
  }
}