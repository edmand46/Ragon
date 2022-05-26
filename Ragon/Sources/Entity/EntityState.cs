using System;
using System.Runtime.Serialization;
using Ragon.Common;

namespace Ragon.Core;

public class EntityState
{
  public bool isDirty { get; private set; }
  public RagonAuthority Authority { get; private set; }
  public int Size => _size;
  
  private int _size = 0;
  private byte[] _data = new byte[2048];
  
  public EntityState(RagonAuthority ragonAuthority)
  {
    Authority = ragonAuthority;
    isDirty = false;
  }

  public ReadOnlySpan<byte> Read()
  {
    return _data.AsSpan().Slice(0, _size);
  }

  public void Write(ref ReadOnlySpan<byte> src)
  {
    src.CopyTo(_data);
    _size = src.Length;
    isDirty = true;
  }
  
  public void Clear()
  {
    isDirty = false;
  }
}