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
using Ragon.Core.Time;
using Ragon.Protocol;
using Ragon.Server;

namespace Ragon.Server;

public class RagonServer : INetworkListener
{
  private readonly Logger _logger = LogManager.GetCurrentClassLogger();
  private readonly INetworkServer _server;
  private readonly Thread _dedicatedThread;
  private readonly Executor _executor;
  private readonly Configuration _configuration;
  private readonly IRagonOperation[] _handlers;
  private readonly RagonBuffer _reader;
  private readonly RagonBuffer _writer;
  private readonly IRagonLobby _lobby;
  private readonly RagonScheduler _scheduler;
  private readonly Dictionary<ushort, RagonContext> _contexts;
  private long _tickrate = 0;
  private Stopwatch _timer;
  
  public RagonServer(INetworkServer server, Configuration configuration)
  {
    _server = server;
    _executor = _server.Executor;
    _configuration = configuration;
    _dedicatedThread = new Thread(Execute);
    _dedicatedThread.IsBackground = true;
    _contexts = new Dictionary<ushort, RagonContext>();
    _lobby = new LobbyInMemory();
    _scheduler = new RagonScheduler();
    
    _reader = new RagonBuffer();
    _writer = new RagonBuffer();
    _tickrate = _configuration.ServerTickRate;
    _timer = new Stopwatch();
    
    _handlers = new IRagonOperation[byte.MaxValue];
    _handlers[(byte) RagonOperation.AUTHORIZE] = new AuthorizationOperation();
    _handlers[(byte) RagonOperation.JOIN_OR_CREATE_ROOM] = new RoomJoinOrCreateOperation();
    _handlers[(byte) RagonOperation.CREATE_ROOM] = new RoomCreateOperation();
    _handlers[(byte) RagonOperation.JOIN_ROOM] = new RoomJoinOperation();
    _handlers[(byte) RagonOperation.LEAVE_ROOM] = new RoomLeaveOperation();
    _handlers[(byte) RagonOperation.LOAD_SCENE] = new SceneLoadOperation();
    _handlers[(byte) RagonOperation.SCENE_LOADED] = new SceneLoadedOperation();
    _handlers[(byte) RagonOperation.CREATE_ENTITY] = new EntityCreateOperation();
    _handlers[(byte) RagonOperation.DESTROY_ENTITY] = new EntityDestroyOperation();
    _handlers[(byte) RagonOperation.REPLICATE_ENTITY_EVENT] = new EntityEventOperation();
    _handlers[(byte) RagonOperation.REPLICATE_ENTITY_STATE] = new EntityStateOperation();
    
    _logger.Trace($"Server Tick Rate: {_configuration.ServerTickRate}");
  }

  public void Execute()
  {
    _timer.Start();
    while (true)
    {
      if (_timer.ElapsedMilliseconds > _tickrate)
      {
        _executor.Update();
        _timer.Restart();
      }
      
      _scheduler.Update();
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
    
    _server.Start(this, networkConfiguration);

    if (executeInDedicatedThread)
      _dedicatedThread.Start();
    else 
      Execute();
  }

  public void Dispose()
  {
    _server.Stop();
    _dedicatedThread.Interrupt();
  }

  public void OnConnected(INetworkConnection connection)
  {
    var lobbyPlayer = new RagonLobbyPlayer(connection);
    var context = new RagonContext(connection, _executor, _lobby, _scheduler, lobbyPlayer);

    _logger.Trace($"Connected: {connection.Id}");
    _contexts.Add(connection.Id, context);
  }

  public void OnDisconnected(INetworkConnection connection)
  {
    if (_contexts.Remove(connection.Id, out var context))
    {
      var room = context.Room;
      if (room != null)
      {
        room.DetachPlayer(context.RoomPlayer);
        _lobby.RemoveIfEmpty(room);
      }
      
      _logger.Trace($"Disconnected: {connection.Id}|{context.LobbyPlayer.Name}|{context.LobbyPlayer.Id}");
    }
    else
    {
      _logger.Trace($"Disconnected: {connection.Id}");
    }
  }

  public void OnTimeout(INetworkConnection connection)
  {
    if (_contexts.Remove(connection.Id, out var context))
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
      if (_contexts.TryGetValue(connection.Id, out var context))
      {
        _writer.Clear();
        _reader.Clear();
        _reader.FromArray(data);
        
        // Console.WriteLine($"{string.Join(",", data.Select(d => d.ToString()))}");
        var operation = _reader.ReadByte();
        _handlers[operation].Handle(context, _reader, _writer);
      }
    }
    catch (Exception ex)
    {
      _logger.Error(ex);
    }
  }
}