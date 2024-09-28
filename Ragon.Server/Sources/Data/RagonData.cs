using Ragon.Protocol;

namespace Ragon.Server.Data;

public class RagonData
{
  private readonly Dictionary<string, byte[]> _data = new Dictionary<string, byte[]>();

  public bool IsDirty { get; private set; }

  public RagonData()
  {
  }

  public void Read(RagonStream buffer)
  {
    var len = buffer.ReadUShort();
    for (int i = 0; i < len; i++)
    {
      var key = buffer.ReadString();
      var valueSize = buffer.ReadUShort();
      if (valueSize > 0)
      {
        var value = buffer.ReadBinary(valueSize);
        _data[key] = value;
      }
      else
      {
        _data[key] = Array.Empty<byte>();
      }
    }
  }

  public void Write(RagonStream buffer)
  {
    buffer.WriteUShort((ushort)_data.Count);
    foreach (var prop in _data)
    {
      buffer.WriteString(prop.Key);
      buffer.WriteUShort((ushort)prop.Value.Length);
      buffer.WriteBinary(prop.Value);
    }

    var toDelete = _data
      .Where(p => p.Value.Length == 0)
      .Select(p => p.Key);

    foreach (var prop in toDelete)
      _data.Remove(prop);

    IsDirty = false;
  }

  public void Snapshot(RagonStream buffer)
  {
    buffer.WriteUShort((ushort)_data.Count);
    foreach (var prop in _data)
    {
      buffer.WriteString(prop.Key);
      buffer.WriteUShort((ushort)prop.Value.Length);
      buffer.WriteBinary(prop.Value);
      
      Console.WriteLine($"Key: {prop.Key} Value: {prop.Value.Length}");
    }
  }
}