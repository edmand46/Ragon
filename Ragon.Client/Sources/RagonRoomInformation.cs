namespace Ragon.Client
{
  public struct RagonRoomInformation
  {
    public string Id;
    public string Scene;
    public int PlayerMax;
    public int PlayerMin;
    public int PlayerCount;
    public Dictionary<string, byte[]> Properties;
  }
}