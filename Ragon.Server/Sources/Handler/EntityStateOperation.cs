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
using Ragon.Server.IO;

namespace Ragon.Server.Handler;

public sealed class EntityStateOperation: BaseOperation
{
  private readonly ILogger _logger = LogManager.GetCurrentClassLogger();

  public EntityStateOperation(RagonBuffer reader, RagonBuffer writer) : base(reader, writer)
  {
  }

  public override void Handle(RagonContext context, NetworkChannel channel)
  {
    var room = context.Room;
    var player = context.RoomPlayer;
    var entitiesCount = Reader.ReadUShort();
    
    for (var entityIndex = 0; entityIndex < entitiesCount; entityIndex++)
    {
      var entityId = Reader.ReadUShort();
      if (room.Entities.TryGetValue(entityId, out var entity) && entity.TryReadState(player, Reader))
      {
        room.Track(entity);
      }
      else
      {
        _logger.Error($"Entity with Id {entityId} not found, replication interrupted");
      }
    }
  }
}