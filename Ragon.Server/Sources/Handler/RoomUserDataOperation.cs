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
using Ragon.Server.Lobby;

namespace Ragon.Server.Handler;

public sealed class RoomUserDataOperation : BaseOperation
{
  private readonly ILogger _logger = LogManager.GetCurrentClassLogger();
  private readonly int _userDataLimit;
  
  public RoomUserDataOperation(
    RagonBuffer reader,
    RagonBuffer writer,
    int userDataLimit
  ) : base(reader, writer)
  {
    _userDataLimit = userDataLimit;
  }

  public override void Handle(RagonContext context, NetworkChannel channel)
  {
    if (context.ConnectionStatus == ConnectionStatus.Unauthorized)
    {
      _logger.Warn($"Player {context.Connection.Id} not authorized for this request");
      return;
    }
    
    var room = context.Room;
    if (room != null)
      room.UserData.Read(Reader);
  }
}