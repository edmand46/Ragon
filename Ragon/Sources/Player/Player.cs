using System;
using System.Collections.Generic;

namespace Ragon.Core
{
  public class Player
  {
    public string Id { get; set; }
    public uint PeerId { get; set; }
    public string PlayerName { get; set; }
    public bool IsLoaded { get; set; }
    
    public List<Entity> Entities;
    public List<ushort> EntitiesIds;
  }
}