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

namespace Ragon.Server.Handler;

public class SceneLoadOperation: IRagonOperation
{
  private readonly Logger _logger = LogManager.GetCurrentClassLogger();
  
  public void Handle(RagonContext context, RagonBuffer reader, RagonBuffer writer)
  {
    var roomOwner = context.Room.Owner;
    var currentPlayer = context.RoomPlayer;
    var room = context.Room;
    var sceneName = reader.ReadString();
    
    if (roomOwner.Connection.Id != currentPlayer.Connection.Id)
    { 
      _logger.Warn("Only owner can change scene!");
      return;
    }
    
    room.UpdateMap(sceneName);
    
    writer.Clear();
    writer.WriteOperation(RagonOperation.LOAD_SCENE);
    writer.WriteString(sceneName);

    var sendData = writer.ToArray();
    foreach (var player in room.PlayerList)
      player.Connection.Reliable.Send(sendData);
  }
}