using System;
using System.Runtime.Serialization;
using Ragon.Common;

namespace Ragon.Core;

public class EntityState
{
  public bool isDirty { get; private set; }
  public RagonAuthority Authority { get; private set; }

  public byte[] Data
  {
    get => Data;
    set
    {
      Data = value;
      isDirty = true;
    }
  }
  
  public EntityState(RagonAuthority ragonAuthority)
  {
    Authority = ragonAuthority;
    isDirty = true;
  }

  public void Clear()
  {
    isDirty = true;
  }
}