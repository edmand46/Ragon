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

using Ragon.Server.Entity;
using Ragon.Server.IO;
using Ragon.Server.Room;

namespace Ragon.Server.Plugin;

public class BaseRoomPlugin: IRoomPlugin
{
  private IRagonRoom _ragonRoom;
  
  public RagonRoomPlayer GetPlayerById(string id)
  {
    var player = _ragonRoom.GetPlayerById(id);
    return player;
  }

  public RagonRoomPlayer GetPlayerByConnection(INetworkConnection connection)
  {
    var player = _ragonRoom.GetPlayerByConnection(connection);
    return player;
  }
  
  public virtual void OnAttached(IRagonRoom room)
  {
    _ragonRoom = room;
  }
  
  public virtual void OnDetached()
  {
    
  }

  #region VIRTUAL
  
  public virtual void Tick(float dt)
  {
    
  }
  
  public virtual bool OnEntityCreate(RagonRoomPlayer creator, RagonEntity entity)
  {
    return true;
  }

  public virtual bool OnEntityRemove(RagonRoomPlayer remover, RagonEntity entity)
  {
    return true;
  }
  
  #endregion
}