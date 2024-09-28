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
using Ragon.Server.Lobby;
using Ragon.Server.Logging;
using Ragon.Server.Plugin;

namespace Ragon.Server.Handler
{
  public sealed class AuthorizationOperation : BaseOperation
  {
    private readonly IRagonLogger _logger = LoggerManager.GetLogger(nameof(AuthorizationOperation));
    private readonly IServerPlugin _serverPlugin;
    private readonly IRagonServer _server;
    private readonly RagonContextObserver _observer;
    private readonly RagonServerConfiguration _configuration;
    private readonly RagonStream _writer;

    public AuthorizationOperation(RagonStream reader,
      RagonStream writer,
      IRagonServer server,
      IServerPlugin serverPlugin,
      RagonContextObserver observer,
      RagonServerConfiguration configuration) : base(reader, writer)
    {
      _serverPlugin = serverPlugin;
      _configuration = configuration;
      _observer = observer;
      _writer = writer;
      _server = server;
    }

    public override void Handle(RagonContext context, NetworkChannel channel)
    {
      if (context.ConnectionStatus == ConnectionStatus.Authorized)
      {
        _logger.Warning("Player already authorized!");
        return;
      }

      if (context.ConnectionStatus == ConnectionStatus.InProcess)
      {
        _logger.Warning("Player already request authorization!");
        return;
      }

      var configuration = _configuration;
      var key = Reader.ReadString();
      var name = Reader.ReadString();
      var payload = Reader.ReadString();

      if (key == configuration.ServerKey)
      {
        var authorizeViaPlugin = _serverPlugin.OnAuthorize(new ConnectionRequest(_server, context.Connection.Id, payload));
        if (authorizeViaPlugin)
          return;

        var id = Guid.NewGuid().ToString();
        Approve(context, new ConnectionResponse(id, name, payload));
      }
      else
      {
        _logger.Warning($"Invalid key for connection {context.Connection.Id}");
        
        Reject(context);
      }
    }

    public void Approve(RagonContext context, ConnectionResponse result)
    {
      var lobbyPlayer = new RagonLobbyPlayer(context.Connection, result.Id, result.Name, result.Payload);
      context.SetPlayer(lobbyPlayer);
      context.ConnectionStatus = ConnectionStatus.Authorized;

      _observer.OnAuthorized(context);

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

      _logger.Trace($"Approved {context.Connection.Id} as {playerId}|{context.LobbyPlayer.Name}");
    }

    public void Reject(RagonContext context)
    {
      _writer.Clear();
      _writer.WriteOperation(RagonOperation.AUTHORIZED_FAILED);

      var sendData = _writer.ToArray();

      context.Connection.Reliable.Send(sendData);
      context.Connection.Close();

      _logger.Trace($"Rejected  Connectin:{context.Connection.Id}");
    }
  }
}