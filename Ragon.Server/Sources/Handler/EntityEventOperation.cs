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

using Ragon.Protocol;
using Ragon.Server.Event;
using Ragon.Server.IO;
using Ragon.Server.Logging;

namespace Ragon.Server.Handler;

public sealed class EntityEventOperation : BaseOperation
{
  private readonly IRagonLogger _logger = LoggerManager.GetLogger(nameof(EntityEventOperation));
  
  public EntityEventOperation(RagonBuffer reader, RagonBuffer writer) : base(reader, writer)
  {
  }

  public override void Handle(RagonContext context, NetworkChannel channel)
  {
    var player = context.RoomPlayer;
    var room = context.Room;
    var entityId = Reader.ReadUShort();

    if (!room.Entities.TryGetValue(entityId, out var ent))
    {
      _logger.Warning($"Entity not found for event with Id {entityId}");
      return;
    }

    var eventId = Reader.ReadUShort();
    var eventMode = (RagonReplicationMode)Reader.ReadByte();
    var targetMode = (RagonTarget)Reader.ReadByte();
    var targetPlayerPeerId = (ushort)0;
    
    if (targetMode == RagonTarget.Player)
      targetPlayerPeerId = Reader.ReadUShort();

    var @event = new RagonEvent(player, eventId);
    @event.Read(Reader);

    if (targetMode == RagonTarget.Player && room.Players.TryGetValue(targetPlayerPeerId, out var targetPlayer))
    {
      ent.ReplicateEvent(player, @event, eventMode, targetPlayer);
      return;
    }

    ent.ReplicateEvent(player, @event, eventMode, targetMode);
  }
}