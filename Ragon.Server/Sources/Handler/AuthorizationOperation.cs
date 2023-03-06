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

public sealed class AuthorizationOperation: IRagonOperation
{
  private Logger _logger = LogManager.GetCurrentClassLogger();
  
  public void Handle(RagonContext context, RagonBuffer reader, RagonBuffer writer)
  {
    if (context.LobbyPlayer.Status == LobbyPlayerStatus.Authorized)
    {
      _logger.Warn("Player already authorized");    
      return;
    }
    
    var key = reader.ReadString();
    var playerName = reader.ReadString();
    var additionalPayload = new RagonPayload();
    additionalPayload.Read(reader);

    context.LobbyPlayer.Name = playerName;
    context.LobbyPlayer.AdditionalData = Array.Empty<byte>();
    context.LobbyPlayer.Status = LobbyPlayerStatus.Authorized;

    var playerId = context.LobbyPlayer.Id;
    
    writer.Clear();
    writer.WriteOperation(RagonOperation.AUTHORIZED_SUCCESS);
    writer.WriteString(playerId);
    writer.WriteString(playerName);
    
    var sendData = writer.ToArray();
    context.Connection.Reliable.Send(sendData);
    
    _logger.Trace($"Connection {context.Connection.Id} as {playerId}|{context.LobbyPlayer.Name} authorized");
  }
}