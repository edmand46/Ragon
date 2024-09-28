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
using Ragon.Server.IO;
using Ragon.Server.Logging;

namespace Ragon.Server.Handler;

public sealed class RoomLeaveOperation: BaseOperation
{
  private readonly IRagonLogger _logger = LoggerManager.GetLogger(nameof(RoomLeaveOperation));
  
  public RoomLeaveOperation(RagonStream reader, RagonStream writer): base(reader, writer)
  {
  }

  public override void Handle(RagonContext context, NetworkChannel channel)
  {
    var room = context.Room;
    var roomPlayer = context.RoomPlayer;
    
    if (room != null)
    {
      var plugin = room.Plugin; 
      
      plugin.OnPlayerLeaved(roomPlayer);
      room.DetachPlayer(roomPlayer);
      
      _logger.Trace($"Player {context.Connection.Id}|{context.LobbyPlayer.Name} leaved from {room.Id}");
    }
  }
}