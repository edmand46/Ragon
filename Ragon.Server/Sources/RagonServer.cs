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
  private readonly BaseOperation[] _handlers;
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
  private readonly RagonLobbyDispatcher _lobbySerializer;
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
    _lobbySerializer = new RagonLobbyDispatcher(_lobby);
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
    
    _scheduler.Run(new RagonActionTimer(SendRoomList, 2.0f));
    _scheduler.Run(new RagonActionTimer(SendPlayerUserData, 0.1f));
    _scheduler.Run(new RagonActionTimer(SendRoomUserData, 0.1f));
    
    _serverPlugin.OnAttached(this);

    _handlers = new BaseOperation[byte.MaxValue];
    _handlers[(byte)RagonOperation.AUTHORIZE] = new AuthorizationOperation(_reader, _writer, _webhooks, contextObserver, configuration);
    _handlers[(byte)RagonOperation.JOIN_OR_CREATE_ROOM] = new RoomJoinOrCreateOperation(_reader, _writer, plugin, _webhooks);
    _handlers[(byte)RagonOperation.CREATE_ROOM] = new RoomCreateOperation(_reader, _writer, plugin, _webhooks);
    _handlers[(byte)RagonOperation.JOIN_ROOM] = new RoomJoinOperation(_reader, _writer, _webhooks);
    _handlers[(byte)RagonOperation.LEAVE_ROOM] = new RoomLeaveOperation(_reader, _writer, _webhooks);
    _handlers[(byte)RagonOperation.LOAD_SCENE] = new SceneLoadOperation(_reader, _writer);
    _handlers[(byte)RagonOperation.SCENE_LOADED] = new SceneLoadedOperation(_reader, _writer);
    _handlers[(byte)RagonOperation.CREATE_ENTITY] = new EntityCreateOperation(_reader, _writer);
    _handlers[(byte)RagonOperation.REMOVE_ENTITY] = new EntityDestroyOperation(_reader, _writer);
    _handlers[(byte)RagonOperation.REPLICATE_ENTITY_EVENT] = new EntityEventOperation(_reader, _writer);
    _handlers[(byte)RagonOperation.REPLICATE_ENTITY_STATE] = new EntityStateOperation(_reader, _writer);
    _handlers[(byte)RagonOperation.TRANSFER_ROOM_OWNERSHIP] = new EntityOwnershipOperation(_reader, _writer);
    _handlers[(byte)RagonOperation.TRANSFER_ENTITY_OWNERSHIP] = new EntityOwnershipOperation(_reader, _writer);
    _handlers[(byte)RagonOperation.TIMESTAMP_SYNCHRONIZATION] = new TimestampSyncOperation(_reader, _writer);
    _handlers[(byte)RagonOperation.REPLICATE_ROOM_EVENT] = new RoomEventOperation(_reader, _writer);
    _handlers[(byte)RagonOperation.REPLICATE_RAW_DATA] = new RoomDataOperation(_reader, _writer);
    _handlers[(byte)RagonOperation.ROOM_DATA_UPDATED] = new RoomUserDataOperation(_reader, _writer, _configuration.LimitUserData);
    _handlers[(byte)RagonOperation.PLAYER_DATA_UPDATED] = new PlayerUserDataOperation(_reader, _writer, _configuration.LimitUserData);

    _logger.Trace($"Server Tick Rate: {_configuration.ServerTickRate}");
  }

  public void Execute()
  {
    _timer.Start();
    while (true)
    {
      if (_timer.ElapsedMilliseconds > _tickRate * 2)
      {
        _logger.Warn($"Slow perfomance: {_timer.ElapsedMilliseconds}");
      }

      if (_timer.ElapsedMilliseconds > _tickRate)
      {
        _timer.Restart();
        _scheduler.Update(_tickRate);

        SendTimestamp();
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
    var context = new RagonContext(connection, _executor, _lobby, _scheduler, _configuration.LimitBufferedEvents);

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
      _logger.Trace($"Disconnected without context: {connection.Id}");
    }
  }

  public void OnTimeout(INetworkConnection connection)
  {
    if (_contextsByConnection.Remove(connection.Id, out var context) && context.ConnectionStatus == ConnectionStatus.Authorized)
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

  public void OnData(INetworkConnection connection, NetworkChannel channel, byte[] data)
  {
    try
    {
      if (_contextsByConnection.TryGetValue(connection.Id, out var context))
      {
        _writer.Clear();
        _reader.Clear();
        _reader.FromArray(data);
        
        var operation = _reader.ReadByte();
        _handlers[operation]?.Handle(context, channel);
      }
    }
    catch (Exception ex)
    {
      _logger.Error(ex);
    }
  }

  public void SendTimestamp()
  {
    var timestamp = RagonTime.CurrentTimestamp();
    var value = new DoubleToUInt
    {
      Double = timestamp,
    };

    _writer.Clear();
    _writer.WriteOperation(RagonOperation.TIMESTAMP_SYNCHRONIZATION);
    _writer.Write(value.Int0, 32);
    _writer.Write(value.Int1, 32);

    var sendData = _writer.ToArray();
    _server.Broadcast(sendData, NetworkChannel.UNRELIABLE);
  }

  public void SendRoomList()
  {
    _lobbySerializer.Write(_writer);

    var sendData = _writer.ToArray();
    foreach (var (_, value) in _contextsByPlayerId)
    {
      if (value.Room == null) // If only in lobby, then send room list data
        value.Connection.Reliable.Send(sendData);
    }
  }

  public void SendPlayerUserData()
  {
    foreach (var (_, value) in _contextsByPlayerId)
    {
      if (value.UserData.IsDirty)
      {
        _writer.Clear();
        _writer.WriteOperation(RagonOperation.PLAYER_DATA_UPDATED);
        _writer.WriteUShort(value.Connection.Id);
        
        value.UserData.Write(_writer);
        
        var sendData = _writer.ToArray();
        _server.Broadcast(sendData, NetworkChannel.RELIABLE);
      }
    }
  }
  
  public void SendRoomUserData()
  {
    foreach (var room in _lobby.Rooms)
    {
      if (room.UserData.IsDirty)
      {
        _writer.Clear();
        _writer.WriteOperation(RagonOperation.ROOM_DATA_UPDATED);
        
        room.UserData.Write(_writer);
        
        var sendData = _writer.ToArray();
        _server.Broadcast(sendData, NetworkChannel.RELIABLE);
      }
    }
  }

  public BaseOperation ResolveHandler(RagonOperation operation)
  {
    return _handlers[(byte)operation];
  }

  public RagonLobbyPlayer? GetPlayerByConnection(INetworkConnection connection)
  {
    return _contextsByConnection.TryGetValue(connection.Id, out var context) ? context.LobbyPlayer : null;
  }

  public RagonLobbyPlayer? GetPlayerById(string playerId)
  {
    return _contextsByPlayerId.TryGetValue(playerId, out var context) ? context.LobbyPlayer : null;
  }
}