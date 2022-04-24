using NetStack.Serialization;

namespace Ragon.Core
{
  public interface IData
  {
    public void Serialize(BitBuffer buffer);
    public void Deserialize(BitBuffer buffer);
  }
}