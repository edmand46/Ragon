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

using Ragon.Server.IO;
using Ragon.Server.Lobby;
using Ragon.Server.Room;

namespace Ragon.Server.Plugin;

public class BaseServerPlugin: IServerPlugin
{
  public IRagonServer Server { get; protected set; }
  
  public virtual void OnAttached(IRagonServer server)
  {
    Server  = server;
  }

  public virtual void OnDetached()
  {
    
  }

  public virtual bool OnRoomCreate(RagonLobbyPlayer player, RagonRoom room)
  {
    return true;
  }

  public virtual bool OnRoomRemove(RagonLobbyPlayer player, RagonRoom room)
  {
    return true;
  }

  public virtual bool OnCommand(string command, string payload)
  {
    return true;
  }

  public IRoomPlugin CreateRoomPlugin(RoomInformation information)
  {
    return new BaseRoomPlugin();
  }
}