using NetStack.Serialization;

namespace Ragon.Common
{
  public interface IRagonSerializable
  {
    public void Serialize(BitBuffer buffer);
    public void Deserialize(BitBuffer buffer);
  }
}