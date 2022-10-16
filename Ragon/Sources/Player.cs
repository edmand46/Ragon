using System;
using System.Collections.Generic;

namespace Ragon.Core
{
  public class Player
  {
    public string Id { get; set; }
    public string PlayerName { get; set; }
    public ushort PeerId { get; set; }
    public bool IsLoaded { get; set; }
    
    public List<Entity> Entities;
    public List<ushort> EntitiesIds;

    public void AttachEntity(Entity entity)
    {
      Entities.Add(entity);
      EntitiesIds.Add((entity.EntityId));
    }

    public void DetachEntity(Entity entity)
    {
      Entities.Remove(entity);
      EntitiesIds.Remove(entity.EntityId);
    }
  }
}