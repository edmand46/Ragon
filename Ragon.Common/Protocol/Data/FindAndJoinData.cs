using NetStack.Serialization;

namespace Ragon.Core
{
  public class FindAndJoinData: IData
  {
    public string Map;
    
    public void Serialize(BitBuffer buffer)
    {
      buffer.AddString(Map);
    }

    public void Deserialize(BitBuffer buffer)
    {
      Map = buffer.ReadString();
    }
  }
}