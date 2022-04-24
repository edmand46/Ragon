using NetStack.Serialization;

namespace Ragon.Core
{
  public class SceneLoadData: IData
  {
    public string Scene;
    
    public void Serialize(BitBuffer buffer)
    {
      buffer.AddString(Scene);
    }

    public void Deserialize(BitBuffer buffer)
    {
      Scene = buffer.ReadString();
    }
  }
}