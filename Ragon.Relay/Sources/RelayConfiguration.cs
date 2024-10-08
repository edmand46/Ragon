using System;

namespace Ragon.Relay
{
  [Serializable]
  public struct RelayConfiguration
  {
    public string ServerKey;
    public string ServerType;
    public string ServerAddress;
    public ushort ServerTickRate;
    public string Protocol;
    public ushort Port;
    public int LimitConnections;
    public int LimitPlayersPerRoom;
    public int LimitRooms;
    public int LimitBufferedEvents;
    public int LimitUserDataSize;
    public int LimitPropertySize;
  }
}