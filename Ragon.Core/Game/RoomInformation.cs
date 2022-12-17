namespace Ragon.Core.Game;

public class RoomInformation
{
  public string Map { get; init; } = "none";
  public int Min { get; init; }
  public int Max { get; init; }

  public override string ToString()
  {
    return $"Map: {Map} Count: {Min}/{Max}";
  }
}