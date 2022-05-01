using NetStack.Serialization;

namespace Ragon.Common
{
  public interface IPacket
  {
    public void Serialize(BitBuffer buffer);
    public void Deserialize(BitBuffer buffer);
  }
}