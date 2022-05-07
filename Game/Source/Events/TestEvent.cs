using NetStack.Serialization;
using Ragon.Common;

namespace Game.Source.Events;

public class TestEvent: IRagonSerializable
{
  public string TestData;
  
  public void Serialize(BitBuffer buffer)
  {
    buffer.AddString(TestData);
  }

  public void Deserialize(BitBuffer buffer)
  {
    TestData = buffer.ReadString();
  }
}