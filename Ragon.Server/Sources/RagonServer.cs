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

using System.Diagnostics;
using NLog;
using Ragon.Protocol;
using Ragon.Server.Handler;
using Ragon.Server.Http;
using Ragon.Server.IO;
using Ragon.Server.Lobby;
using Ragon.Server.Plugin;
using Ragon.Server.Plugin.Web;
using Ragon.Server.Time;

namespace Ragon.Server;

public class RagonServer : IRagonServer, INetworkListener
{
  private readonly Logger _logger = LogManager.GetCurrentClassLogger();
  private readonly INetworkServer _server;
  private readonly IRagonOperation[] _handlers;
  private readonly IRagonLobby _lobby;
  private readonly IServerPlugin _serverPlugin;
  private readonly Thread _dedicatedThread;
  private readonly Executor _executor;
  private readonly RagonServerConfiguration _configuration;
  private readonly RagonWebHookPlugin _webhooks;
  private readonly RagonHttpServer _httpServer;
  private readonly RagonBuffer _reader;
  private readonly RagonBuffer _writer;
  private readonly RagonScheduler _scheduler;
  private readonly Dictionary<ushort, RagonContext> _contextsByConnection;
  private readonly Dictionary<string, RagonContext> _contextsByPlayerId;
  private readonly Stopwatch _timer;
  private readonly long _tickRate = 0;
  
  public RagonServer(
    INetworkServer server,
    IServerPlugin plugin,
    RagonServerConfiguration configuration)
  {
    _server = server;
    _executor = _server.Executor;
    _configuration = configuration;
    _serverPlugin = plugin;
    _contextsByConnection = new Dictionary<ushort, RagonContext>();
    _contextsByPlayerId = new Dictionary<string, RagonContext>();
    _lobby = new LobbyInMemory();
    _scheduler = new RagonScheduler();
    _webhooks = new RagonWebHookPlugin(this, configuration);
    _dedicatedThread = new Thread(Execute);
    _dedicatedThread.IsBackground = true;
    _httpServer = new RagonHttpServer(_executor, plugin);
    _reader = new RagonBuffer();
    _writer = new RagonBuffer();
    _tickRate = 1000 / _configuration.ServerTickRate;
    _timer = new Stopwatch();
    
    var contextObserver = new RagonContextObserver(_contextsByPlayerId);
    
    _serverPlugin.OnAttached(this);
    
    _handlers = new IRagonOperation[byte.MaxValue];
    _handlers[(byte) RagonOperation.AUTHORIZE] = new AuthorizationOperation(_webhooks, contextObserver, _writer);
    _handlers[(byte) RagonOperation.JOIN_OR_CREATE_ROOM] = new RoomJoinOrCreateOperation(plugin, _webhooks);
    _handlers[(byte) RagonOperation.CREATE_ROOM] = new RoomCreateOperation(plugin, _webhooks);
    _handlers[(byte) RagonOperation.JOIN_ROOM] = new RoomJoinOperation(_webhooks);
    _handlers[(byte) RagonOperation.LEAVE_ROOM] = new RoomLeaveOperation(_webhooks);
    _handlers[(byte) RagonOperation.LOAD_SCENE] = new SceneLoadOperation();
    _handlers[(byte) RagonOperation.SCENE_LOADED] = new SceneLoadedOperation();
    _handlers[(byte) RagonOperation.CREATE_ENTITY] = new EntityCreateOperation();
    _handlers[(byte) RagonOperation.REMOVE_ENTITY] = new EntityDestroyOperation();
    _handlers[(byte) RagonOperation.REPLICATE_ENTITY_EVENT] = new EntityEventOperation();
    _handlers[(byte) RagonOperation.REPLICATE_ENTITY_STATE] = new EntityStateOperation();
    _handlers[(byte) RagonOperation.TRANSFER_ROOM_OWNERSHIP] = new EntityOwnershipOperation();
    _handlers[(byte) RagonOperation.TRANSFER_ENTITY_OWNERSHIP] = new EntityOwnershipOperation();
    
    _logger.Trace($"Server Tick Rate: {_configuration.ServerTickRate}");
  }

  public void Execute()
  {
    _timer.Start();
    while (true)
    {
      if (_timer.ElapsedMilliseconds > _tickRate)
      {
        _scheduler.Update(_timer.ElapsedMilliseconds / 1000.0f);
        _timer.Restart();
      }
      
      _executor.Update();
      _server.Update();
      Thread.Sleep(1);
    }
  }

  public void Start(bool executeInDedicatedThread = false)
  {
    var networkConfiguration = new NetworkConfiguration()
    {
      LimitConnections = _configuration.LimitConnections,
      Protocol = RagonVersion.Parse(_configuration.GameProtocol),
      Address = "0.0.0.0",
      Port = _configuration.Port,
    };
    
    _httpServer.Start(_configuration);
    _server.Start(this, networkConfiguration);

    if (executeInDedicatedThread)
      _dedicatedThread.Start();
    else 
      Execute();
  }

  public void Dispose()
  {
    _serverPlugin.OnDetached();
    _server.Stop();
    _dedicatedThread.Interrupt();
  }

  public void OnConnected(INetworkConnection connection)
  {
    var context = new RagonContext(connection, _configuration, _executor, _lobby, _scheduler);
   
    _logger.Trace($"Connected: {connection.Id}");
    _contextsByConnection.Add(connection.Id, context);
  }

  public void OnDisconnected(INetworkConnection connection)
  {
    if (_contextsByConnection.Remove(connection.Id, out var context))
    {
      var room = context.Room;
      if (room != null)
      {
        room.DetachPlayer(context.RoomPlayer);
        if (_lobby.RemoveIfEmpty(room)) 
          _webhooks.RoomRemoved(context, room);
      }
      
      _logger.Trace($"Disconnected: {connection.Id}");
    }
    else
    {
      _logger.Trace($"Disconnected: {connection.Id}");
    }
  }

  public void OnTimeout(INetworkConnection connection)
  {
    if (_contextsByConnection.Remove(connection.Id, out var context))
    {
      var room = context.Room;
      if (room != null)
      {
        room.DetachPlayer(context.RoomPlayer);
        _lobby.RemoveIfEmpty(room);
      }
      
      _logger.Trace($"Timeout: {connection.Id}|{context.LobbyPlayer.Name}|{context.LobbyPlayer.Id}");
    }
    else
    {
      _logger.Trace($"Timeout: {connection.Id}");
    }
  }

  public void OnData(INetworkConnection connection, byte[] data)
  {
    try
    {
      if (_contextsByConnection.TryGetValue(connection.Id, out var context))
      {
        _writer.Clear();
        _reader.Clear();
        _reader.FromArray(data);
        
        var operation = _reader.ReadByte();
        _handlers[operation].Handle(context, _reader, _writer);
      }
    }
    catch (Exception ex)
    {
      _logger.Error(ex);
    }
  }

  public IRagonOperation ResolveOperation(RagonOperation operation)
  {
    return _handlers[(byte)operation];
  }

  public RagonLobbyPlayer? GetPlayerByConnection(INetworkConnection connection)
  {
    return _contextsByConnection.TryGetValue(connection.Id, out var context) ? 
      context.LobbyPlayer : 
      null;
  }

  public RagonLobbyPlayer? GetPlayerById(string playerId)
  {
    return _contextsByPlayerId.TryGetValue(playerId, out var context) ? 
      context.LobbyPlayer :
      null;
  }
}