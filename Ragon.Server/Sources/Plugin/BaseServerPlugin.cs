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
using Ragon.Server.Handler;
using Ragon.Server.IO;
using Ragon.Server.Lobby;
using Ragon.Server.Room;

namespace Ragon.Server.Plugin
{
  public class ConnectionRequest
  {
    public ConnectionRequest(
      IRagonServer server,
      ushort connectionId,
      string payload
    )
    {
      _server = server;
      PeerID = connectionId;
      Payload = payload;
    }

    public ushort PeerID { get; private set; }
    public string Payload { get; private set; }

    private readonly IRagonServer _server;

    public void Approve(string id, string name, string payload)
    {
      var ctx = _server.GetContextByConnectionId(PeerID);
      if (ctx == null)
        return;

      var operation = (AuthorizationOperation)_server.ResolveHandler(RagonOperation.AUTHORIZE);
      operation.Approve(ctx, new ConnectionResponse(id, name, payload));
    }

    public void Reject()
    {
      var ctx = _server.GetContextByConnectionId(PeerID);
      if (ctx == null)
        return;

      var operation = (AuthorizationOperation)_server.ResolveHandler(RagonOperation.AUTHORIZE);
      operation.Reject(ctx);
    }
  }

  public class ConnectionResponse
  {
    public string Id { get; }
    public string Name { get; }
    public string Payload { get; }

    public ConnectionResponse(string id, string name, string payload)
    {
      Id = id;
      Name = name;
      Payload = payload;
    }
  }


  public class BaseServerPlugin : IServerPlugin
  {
    public IRagonServer Server { get; protected set; }

    public virtual void OnAttached(IRagonServer server)
    {
      Server = server;
    }

    public virtual void OnDetached()
    {
    }

    public virtual bool OnAuthorize(ConnectionRequest request)
    {
      return false;
    }

    public virtual bool OnRoomCreate(RagonLobbyPlayer player, RagonRoom room)
    {
      return true;
    }

    public virtual bool OnRoomRemove(RagonLobbyPlayer player, RagonRoom room)
    {
      return true;
    }

    public bool OnRoomJoined(RagonRoomPlayer player, RagonRoom room)
    {
      return true;
    }

    public bool OnRoomLeaved(RagonRoomPlayer player, RagonRoom room)
    {
      return true;
    }

    public virtual bool OnCommand(string command, string payload)
    {
      return true;
    }

    public virtual IRoomPlugin CreateRoomPlugin(RoomInformation information)
    {
      return new BaseRoomPlugin();
    }
  }
}