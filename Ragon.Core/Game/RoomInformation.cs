namespace Ragon.Core.Game;

public class RoomInformation
{
  public string Map { get; set; }
  public int Min { get; set; }
  public int Max { get; set; }

  public override string ToString()
  {
    return $"Map: {Map} Count: {Min}/{Max}";
  }
}