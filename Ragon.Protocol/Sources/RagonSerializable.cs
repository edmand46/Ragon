namespace Ragon.Common
{
  public interface IRagonSerializable
  {
    public void Serialize(RagonSerializer serializer);
    public void Deserialize(RagonSerializer serializer);
  }
}