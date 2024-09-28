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


using Ragon.Protocol;

namespace  Ragon.Client.Replication;

internal class EntityRemoveHandler
{
  private readonly RagonEntityCache _entityCache;
  
  public EntityRemoveHandler(RagonEntityCache entityCache)
  {
    _entityCache = entityCache;
  }
  
  public void Handle(RagonBuffer reader)
  {
    var entityId = reader.ReadUShort();
    // var payload = new RagonPayload(reader.Capacity);
    // payload.Read(reader);
    
    // _entityCache.OnDestroy(entityId, payload);
  }
}