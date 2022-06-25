using System;

namespace Ragon.Core
{
  [Serializable]
  public struct Configuration
  {
    public string Key;
    public int StatisticsInterval;
    public ushort SendRate;
    public ushort Port;
    public int SkipTimeout;
    public int MaxConnections;
    public int MaxPlayersPerRoom;
    public int MaxRooms;
  }
}