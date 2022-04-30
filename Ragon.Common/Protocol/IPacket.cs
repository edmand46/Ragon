using NetStack.Serialization;

namespace Ragon.Core
{
  public interface IPacket
  {
    public void Serialize(BitBuffer buffer);
    public void Deserialize(BitBuffer buffer);
  }
}