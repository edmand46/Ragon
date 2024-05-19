/*
 * Copyright 2023 Eduard Kargin <kargin.eduard@gmail.com>
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

using Ragon.Server.Data;
using Ragon.Server.IO;
using Ragon.Server.Lobby;
using Ragon.Server.Time;
using Ragon.Server.Room;

namespace Ragon.Server;

public class RagonContext
{
  public ConnectionStatus ConnectionStatus { get; set; }
  public INetworkConnection Connection { get; }
  public IRagonLobby Lobby { get; private set; }
  public RagonLobbyPlayer? LobbyPlayer { get; private set; }
  public RagonRoom Room { get; private set; }
  public RagonRoomPlayer? RoomPlayer { get; private set; }
  public RagonData UserData { get; private set; }
  public RagonScheduler Scheduler { get; private set; }

  public int LimitBufferedEvents { get; private set; }
  
  public RagonContext(
    INetworkConnection connection,
    IRagonLobby lobby,
    RagonScheduler scheduler,
    int limitBufferedEvents)
  {
    ConnectionStatus = ConnectionStatus.Unauthorized;
    LimitBufferedEvents = limitBufferedEvents;
    Connection = connection;
    Lobby = lobby;
    Scheduler = scheduler;
    UserData = new RagonData();
  }

  internal void SetPlayer(RagonLobbyPlayer player)
  {
    LobbyPlayer = player;
  }

  internal void SetRoom(RagonRoom room, RagonRoomPlayer player)
  {
    Room?.DetachPlayer(RoomPlayer);

    Room = room;
    RoomPlayer = player;

    Room.AttachPlayer(RoomPlayer);
  }
}