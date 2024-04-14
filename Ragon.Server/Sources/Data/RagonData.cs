using Ragon.Protocol;

namespace Ragon.Server.Data;

public class RagonData
{
  private byte[] _data = Array.Empty<byte>();
  public bool IsDirty { get; set; }
  public byte[] Data
  {
    get => _data;
    set
    {
      _data = value;
      IsDirty = true;
    }
  }

  public RagonData(byte[] data)
  {
    _data = data;
  }
}