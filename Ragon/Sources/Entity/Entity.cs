using System.Collections.Generic;

namespace Ragon.Core;

public class Entity
{
  private static int _idGenerator = 0;
  public int EntityId { get; private set; }
  public uint OwnerId { get; private set; }
  public byte[] State { get; set; }
  public Dictionary<int, byte[]> Properties { get; set; }
  
  public Entity(uint ownerId)
  {
    OwnerId = ownerId;
    EntityId = _idGenerator++;
  }
}