using System;

namespace Ragon.Core
{
  [Serializable]
  public struct Configuration
  {
    public string Key;
    public string Protocol;
    public int StatisticsInterval;
    public ushort SendRate;
    public ushort Port;
    public int SkipTimeout;
    public int ReconnectTimeout;
    public int MaxConnections;
    public int MaxPlayersPerRoom;
    public int MaxRooms;
  }
}