/*
 * Copyright 2023-2024 Eduard Kargin <kargin.eduard@gmail.com>
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

namespace Ragon.Server;

public enum ServerType
{
  ENET,
  WEBSOCKET,
}

[Serializable]
public struct RagonServerConfiguration
{
  public string ServerKey;
  public ushort ServerTickRate;
  public string Protocol;
  public ushort Port;
  public int LimitConnections;
  public int LimitPlayersPerRoom;
  public int LimitRooms;
  public int LimitBufferedEvents;
  public int LimitUserData;

  private static Dictionary<string, ServerType> _serverTypes = new Dictionary<string, ServerType>()
  {
    { "enet", Server.ServerType.ENET },
    { "websocket", Server.ServerType.WEBSOCKET }
  };
  
  public static ServerType GetServerType(string type) => _serverTypes[type];
}