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

public sealed class EntityOwnershipOperation : BaseOperation
{
  private readonly Logger _logger = LogManager.GetCurrentClassLogger();
  
  public EntityOwnershipOperation(RagonBuffer reader, RagonBuffer writer) : base(reader, writer)
  {
  }

  public override void Handle(RagonContext context, NetworkChannel channel)
  {
    var currentOwner = context.RoomPlayer;
    var room = context.Room;
    
    var entityId = Reader.ReadUShort();
    var playerPeerId = Reader.ReadUShort();

    if (!room.Entities.TryGetValue(entityId, out var entity))
    {
      _logger.Error($"Entity not found with id {entityId}");
      return;
    }
    
    if (entity.Owner.Connection.Id != currentOwner.Connection.Id)
    {
      _logger.Error($"Player not owner of entity with id {entityId}");
      return;
    } 

    if (!room.Players.TryGetValue(playerPeerId, out var nextOwner))
    {
      _logger.Error($"Player not found with id {playerPeerId}");
      return;
    }    
    
    currentOwner.Entities.Remove(entity);
    nextOwner.Entities.Add(entity);
    
    entity.Attach(nextOwner);
    
    _logger.Trace($"Entity {entity.Id} next owner {nextOwner.Connection.Id}");
    
    Writer.Clear();
    Writer.WriteOperation(RagonOperation.OWNERSHIP_ENTITY_CHANGED);
    Writer.WriteUShort(playerPeerId);
    Writer.WriteUShort(1);
    Writer.WriteUShort(entity.Id);
    
    var sendData = Writer.ToArray();
    foreach (var player in room.PlayerList)
      player.Connection.Reliable.Send(sendData);
  }
}