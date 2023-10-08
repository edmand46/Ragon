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

using NLog;
using Ragon.Protocol;
using Ragon.Server.Entity;

namespace Ragon.Server.Handler;

public sealed class EntityDestroyOperation: BaseOperation
{
  private readonly Logger _logger = LogManager.GetCurrentClassLogger();
  
  public EntityDestroyOperation(RagonBuffer reader, RagonBuffer writer) : base(reader, writer)
  {
  }

  public override void Handle(RagonContext context)
  {
    var player = context.RoomPlayer;
    var room = context.Room;
    var entityId = Reader.ReadUShort();
    
    if (room.Entities.TryGetValue(entityId, out var entity))
    {
      var payload = new RagonPayload();
      payload.Read(Reader);
      
      room.DetachEntity(entity);
      player.DetachEntity(entity);
      
      entity.Destroy();
      
      _logger.Trace($"Player {context.Connection.Id}|{context.LobbyPlayer.Name} destoyed entity {entity.Id}");
    }
  }
}