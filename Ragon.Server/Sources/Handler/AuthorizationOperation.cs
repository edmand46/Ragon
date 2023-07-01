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
using Ragon.Server.Lobby;
using Ragon.Server.Plugin;
using Ragon.Server.Plugin.Web;


namespace Ragon.Server.Handler;

public sealed class AuthorizationOperation: IRagonOperation
{
  private Logger _logger = LogManager.GetCurrentClassLogger();
  private readonly RagonWebHookPlugin _ragonWebHook;
  private readonly RagonContextObserver _contextObserver;
  private readonly RagonBuffer _writer;
  
  public AuthorizationOperation(
    RagonWebHookPlugin ragonWebHook,
    RagonContextObserver contextObserver,
    RagonBuffer writer)
  {
    _ragonWebHook = ragonWebHook;
    _contextObserver = contextObserver;
    _writer = writer;
  }
  
  public void Handle(RagonContext context, RagonBuffer reader, RagonBuffer writer)
  {
    if (context.ConnectionStatus == ConnectionStatus.Authorized)
    {
      _logger.Warn("Player already authorized!");    
      return;
    }

    if (context.ConnectionStatus == ConnectionStatus.InProcess)
    {
      _logger.Warn("Player already request authorization!");    
      return;
    }

    var configuration = context.Configuration; 
    var key = reader.ReadString();
    var name = reader.ReadString();
    var payload = reader.ReadString();
    
    if (key == configuration.ServerKey)
    {
      if (_ragonWebHook.RequestAuthorization(context, name, payload)) 
        return;
      
      var lobbyPlayer = new RagonLobbyPlayer(context.Connection, Guid.NewGuid().ToString(), name, payload);
      context.SetPlayer(lobbyPlayer);
      
      Approve(context);
    }
    else
    {
      Reject(context);
    }
  }

  public void Approve(RagonContext context)
  {
    context.ConnectionStatus  = ConnectionStatus.Authorized;

    _contextObserver.OnAuthorized(context);
    
    var playerId = context.LobbyPlayer.Id;
    var playerName = context.LobbyPlayer.Name;
    var playerPayload = context.LobbyPlayer.Payload;

    _writer.Clear();
    _writer.WriteOperation(RagonOperation.AUTHORIZED_SUCCESS);
    _writer.WriteString(playerId);
    _writer.WriteString(playerName);
    _writer.WriteString(playerPayload);
    
    var sendData = _writer.ToArray();
    context.Connection.Reliable.Send(sendData);
    
    _logger.Trace($"Connection {context.Connection.Id} as {playerId}|{context.LobbyPlayer.Name} authorized");   
  }

  public void Reject(RagonContext context)
  {
    _writer.Clear();
    _writer.WriteOperation(RagonOperation.AUTHORIZED_FAILED);

    var sendData = _writer.ToArray();
    
    context.Connection.Reliable.Send(sendData);
    context.Connection.Close();
      
    _logger.Trace($"Connection {context.Connection.Id}");
  }
}