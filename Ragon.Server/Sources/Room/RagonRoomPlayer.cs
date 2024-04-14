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
using Ragon.Server.Entity;
using Ragon.Server.IO;

namespace Ragon.Server.Room;

public class RagonRoomPlayer
{
  public INetworkConnection Connection { get; }
  public string Id { get; }
  public string Name { get; }
  public bool IsLoaded { get; private set; }
  public double Timestamp { get; private set; }
  public RagonRoom Room { get; private set; }
  public RagonEntityCache Entities { get; private set; }
  
  public RagonData UserData { get; private set; }
  
  public RagonRoomPlayer(INetworkConnection connection, string id, string name)
  {
    Id = id;
    Name = name;
    Connection = connection;
    Entities = new RagonEntityCache();
    UserData = new RagonData(Array.Empty<byte>());
  }

  public void AttachEntity(RagonEntity entity)
  {
    Entities.Add(entity);
  }

  public void DetachEntity(RagonEntity entity)
  {
    Entities.Remove(entity);
  }
  
  internal void OnAttached(RagonRoom room)
  {
    Room = room;
  }

  internal  void OnDetached()
  {
    Room = null!;
  }

  internal void SetReady()
  {
    IsLoaded = true;
  }

  internal void UnsetReady()
  {
    IsLoaded = false;
  }

  internal void SetTimestamp(double time)
  {
    Timestamp = time;
  }
}