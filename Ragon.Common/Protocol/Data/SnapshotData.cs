using NetStack.Serialization;
using Ragon.Common;

namespace Ragon.Core
{

  public class EntityData: IData
  {
    public int EntityId;
    public byte[] State;
    public void Serialize(BitBuffer buffer)
    {
      buffer.AddInt(EntityId);
      buffer.AddBytes(State);
    }

    public void Deserialize(BitBuffer buffer)
    {
      EntityId = buffer.ReadInt();
      State = buffer.ReadBytes();
    }
  }
  public class SnapshotData: IData
  {
    public EntityData[] Entities;
    public void Serialize(BitBuffer buffer)
    {
      buffer.AddInt(Entities.Length);
      foreach (var entityData in Entities)
        entityData.Serialize(buffer);
    }
    public void Deserialize(BitBuffer buffer)
    {
      var entitiesSize = buffer.ReadInt();
      var i = 0;

      Entities = new EntityData[entitiesSize];
      while (i < entitiesSize)
      {
        Entities[i] = new EntityData();
        Entities[i].Deserialize(buffer);
      }
    }
  }
}