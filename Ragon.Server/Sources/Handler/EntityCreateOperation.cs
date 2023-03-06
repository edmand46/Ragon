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

namespace Ragon.Server;

public sealed class EntityCreateOperation : IRagonOperation
{
  private Logger _logger = LogManager.GetCurrentClassLogger();

  public void Handle(RagonContext context, RagonBuffer reader, RagonBuffer writer)
  {
    var player = context.RoomPlayer;
    var room = context.Room;
    var attachId = reader.ReadUShort();
    var entityType = reader.ReadUShort();
    var eventAuthority = (RagonAuthority) reader.ReadByte();
    var propertiesCount = reader.ReadUShort();

    var entity = new RagonEntity(player, entityType, 0, attachId, eventAuthority);
    for (var i = 0; i < propertiesCount; i++)
    {
      var propertyType = reader.ReadBool();
      var propertySize = reader.ReadUShort();

      entity.State.AddProperty(new RagonProperty(propertySize, propertyType));
    }

    if (reader.Capacity > 0)
      entity.Payload.Read(reader);

    room.AttachEntity(entity);
    player.AttachEntity(entity);

    entity.Create();
    
    _logger.Trace($"Player {context.Connection.Id}|{context.LobbyPlayer.Name} created entity {entity.Id}:{entity.Type}");
  }
}