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

public sealed class RoomLeaveOperation: IRagonOperation
{
  private Logger _logger = LogManager.GetCurrentClassLogger();
  public void Handle(RagonContext context, RagonBuffer reader, RagonBuffer writer)
  {
    var room = context.Room;
    var roomPlayer = context.RoomPlayer;
    if (room != null)
    { 
      context.Room?.DetachPlayer(roomPlayer);
      _logger.Trace($"Player {context.Connection.Id}|{context.LobbyPlayer.Name} leaved from {room.Id}");
    }
  }
}