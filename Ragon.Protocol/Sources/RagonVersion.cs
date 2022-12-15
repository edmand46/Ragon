namespace Ragon.Common
{
  public static class RagonVersion
  {
    public static uint Parse(string version)
    {
      var strings = version.Split(".");
      if (strings.Length < 3)
        return 0;
      
      var parts = new uint[] {0, 0, 0};
      for (int i = 0; i < parts.Length; i++)
      {
        if (!uint.TryParse(strings[i], out var v))
          return 0;

        parts[i] = v;
      }
      
      return (parts[0] << 16) | (parts[1] << 8) | parts[2];
    }

    public static string Parse(uint version)
    {
      return (version >> 16 & 0xFF) + "." + (version >> 8 & 0xFF) + "." + (version & 0xFF);
    }
  }
}