using System;
using Ragon.Common;

namespace Ragon.Core;

public class EntityProperty
{
    public int Size { get; set; }
    public int Capacity { get; set; }
    public bool IsDirty { get; private set; }
    public bool IsFixed { get; private set; }
    private byte[] _data;

    public EntityProperty(int size, bool isFixed)
    {
        Capacity = 512;
        Size = size;
        IsFixed = isFixed;
        IsDirty = true;
        
        _data = new byte[Capacity];
    }
    
    public ReadOnlySpan<byte> Read()
    {
        var dataSpan = _data.AsSpan();
        
        return dataSpan.Slice(0, Size);
    }

    public void Write(ref ReadOnlySpan<byte> src)
    {
        src.CopyTo(_data);
        IsDirty = true;
    }
  
    public void Clear()
    {
        IsDirty = false;
    }
}