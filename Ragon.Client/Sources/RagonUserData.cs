using Ragon.Protocol;

namespace Ragon.Client
{
  public class RagonUserData
  {
    public byte[] this[string key]
    {
      get => _properties[key];
      set
      {
        _properties[key] = value;
        
        _dirty = true;
      }
    }
    public bool Dirty => _dirty;

    private bool _dirty = false;
    private readonly Dictionary<string, byte[]> _properties = new();
    
    public RagonUserData()
    {
    }
    
    internal void Read(RagonBuffer buffer)
    {
      _properties.Clear();
      
      var len = buffer.ReadUShort();
      for (int i = 0; i < len; i++)
      {
        var key = buffer.ReadString();
        var valueSize = buffer.ReadUShort();
        var value = buffer.ReadBytes(valueSize);

        _properties[key] = value;
      }  
    }

    internal  void Write(RagonBuffer buffer)
    {
      buffer.WriteUShort((ushort)_properties.Count);
      foreach (var property in _properties)
      {
        buffer.WriteString(property.Key);
        buffer.WriteUShort((ushort) property.Value.Length);
        buffer.WriteBytes(property.Value);
      }
      
      _dirty = false;
    }
  }
}