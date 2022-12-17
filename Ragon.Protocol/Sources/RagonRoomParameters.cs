
namespace Ragon.Common
{
  public class RagonRoomParameters: IRagonSerializable
  {
    public string Map { get; set; }
    public int Min { get; set; } 
    public int Max { get; set; } 
    
    public void Serialize(RagonSerializer buffer)
    {
      buffer.WriteString(Map);
      buffer.WriteInt(Min);
      buffer.WriteInt(Max);
    }

    public void Deserialize(RagonSerializer buffer)
    {
      Map = buffer.ReadString();
      Min = buffer.ReadInt();
      Max = buffer.ReadInt();
    }
  }
}