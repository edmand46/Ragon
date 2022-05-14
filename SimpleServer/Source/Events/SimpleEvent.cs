using NetStack.Serialization;
using Ragon.Common;

namespace Game.Source.Events;

public class SimpleEvent: IRagonSerializable
{
  public string Name;
  
  public void Serialize(BitBuffer buffer)
  {
    buffer.AddString(Name);
  }

  public void Deserialize(BitBuffer buffer)
  {
    Name = buffer.ReadString();
  }
}