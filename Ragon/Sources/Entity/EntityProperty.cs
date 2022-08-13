using System;

namespace Ragon.Core;

public class EntityProperty
{
    public int Size => _data.Length;
    public bool IsDirty => _dirty;
    
    private bool _dirty;
    private byte[] _data;
    
    public EntityProperty(int size)
    {
        _data = new byte[size];
    }
    
    public ReadOnlySpan<byte> Read()
    {
        return _data.AsSpan();
    }

    public void Write(ref ReadOnlySpan<byte> src)
    {
        src.CopyTo(_data);
        _dirty = true;
    }
  
    public void Clear()
    {
        _dirty = false;
    }
}