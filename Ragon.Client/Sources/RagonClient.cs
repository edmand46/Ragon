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

using Ragon.Protocol;

namespace Ragon.Client
{
  public sealed class RagonClient
  {
    private readonly INetworkConnection _connection;
    private readonly NetworkStatistics _stats;
    private IRagonEntityListener _entityListener;
    private IRagonSceneCollector _sceneCollector;
    private IHandler[] _handlers;
    private RagonBuffer _readBuffer;
    private RagonBuffer _writeBuffer;
    private RagonRoom _room;
    private RagonSession _session;
    private RagonListenerList listeners;
    private RagonPlayerCache _playerCache;
    private RagonEntityCache _entityCache;
    private RagonEventCache _eventCache;
    private RagonStatus _status;

    private double _serverTimestamp;
    private float _replicationRate = 0;
    private float _replicationTime = 0;

    public double ServerTimestamp => _serverTimestamp;
    public IRagonConnection Connection => _connection;
    public RagonStatus Status => _status;
    public RagonSession Session => _session;
    public RagonEventCache Event => _eventCache;
    public RagonEntityCache Entity => _entityCache;
    public NetworkStatistics Statistics => _stats;
    public RagonRoom Room => _room;


    internal RagonBuffer Buffer => _writeBuffer;
    internal INetworkChannel Reliable => _connection.Reliable;
    internal INetworkChannel Unreliable => _connection.Unreliable;

    #region PUBLIC

    public RagonClient(INetworkConnection connection, int rate)
    {
      listeners = new RagonListenerList(this);
      
      _connection = connection;
      _connection.OnData += OnData;
      _connection.OnConnected += OnConnected;
      _connection.OnDisconnected += OnDisconnected;
      
      _replicationRate = (1000.0f / rate) / 1000.0f;
      _replicationTime = 0;

      _eventCache = new RagonEventCache();
      _stats = new NetworkStatistics();
      _status = RagonStatus.DISCONNECTED;
    }


    public void Configure(IRagonSceneCollector sceneCollector)
    {
      _sceneCollector = sceneCollector;
    }

    public void Configure(IRagonEntityListener listener)
    {
      _entityListener = listener;
    }

    public void Connect(string address, ushort port, string protocol)
    {
      if (_sceneCollector == null)
      {
        RagonLog.Error("Scene collector is not defined!");
        return;
      }

      if (_entityListener == null)
      {
        RagonLog.Error("Entity Listener is not defined!");
        return;
      }

      _writeBuffer = new RagonBuffer();
      _readBuffer = new RagonBuffer();
      _session = new RagonSession(this, _writeBuffer);

      _playerCache = new RagonPlayerCache();
      _entityCache = new RagonEntityCache(this, _playerCache, _sceneCollector);

      _handlers = new IHandler[byte.MaxValue];
      _handlers[(byte)RagonOperation.AUTHORIZED_SUCCESS] = new AuthorizeSuccessHandler(this, listeners);
      _handlers[(byte)RagonOperation.AUTHORIZED_FAILED] = new AuthorizeFailedHandler(listeners);
      _handlers[(byte)RagonOperation.JOIN_SUCCESS] = new JoinSuccessHandler(this, listeners, _playerCache, _entityCache);
      _handlers[(byte)RagonOperation.JOIN_FAILED] = new JoinFailedHandler(listeners);
      _handlers[(byte)RagonOperation.LEAVE_ROOM] = new LeaveRoomHandler(this, listeners, _entityCache);
      _handlers[(byte)RagonOperation.OWNERSHIP_ROOM_CHANGED] = new OwnershipRoomHandler(listeners, _playerCache, _entityCache);
      _handlers[(byte)RagonOperation.OWNERSHIP_ENTITY_CHANGED] = new EntityOwnershipHandler(listeners, _playerCache, _entityCache);
      _handlers[(byte)RagonOperation.PLAYER_JOINED] = new PlayerJoinHandler(_playerCache, listeners);
      _handlers[(byte)RagonOperation.PLAYER_LEAVED] = new PlayerLeftHandler(_entityCache, _playerCache, listeners);
      _handlers[(byte)RagonOperation.LOAD_SCENE] = new SceneLoadHandler(this, listeners);
      _handlers[(byte)RagonOperation.CREATE_ENTITY] = new EntityCreateHandler(this, _playerCache, _entityCache, _entityListener);
      _handlers[(byte)RagonOperation.REMOVE_ENTITY] = new EntityRemoveHandler(_entityCache);
      _handlers[(byte)RagonOperation.REPLICATE_ENTITY_STATE] = new StateEntityHandler(_entityCache);
      _handlers[(byte)RagonOperation.REPLICATE_ENTITY_EVENT] = new EntityEventHandler(_playerCache, _entityCache);
      _handlers[(byte)RagonOperation.REPLICATE_ROOM_EVENT] = new RoomEventHandler(this, _playerCache);
      _handlers[(byte)RagonOperation.SNAPSHOT] = new SnapshotHandler(this, listeners, _entityCache, _playerCache, _entityListener);
      _handlers[(byte)RagonOperation.TIMESTAMP_SYNCHRONIZATION] = new TimestampHandler(this);
      _handlers[(byte)RagonOperation.REPLICATE_RAW_DATA] = new RoomDataHandler(_playerCache, listeners);

      var protocolRaw = RagonVersion.Parse(protocol);
      _connection.Connect(address, port, protocolRaw);
    }

    public void Disconnect()
    {
      _status = RagonStatus.DISCONNECTED;
      _room.Cleanup();
      _connection.Disconnect();

      OnDisconnected(RagonDisconnect.MANUAL);
    }

    public void Update(float dt)
    {
      if (_status != RagonStatus.DISCONNECTED)
      {
        _replicationTime += dt;
        if (_replicationTime >= _replicationRate)
        {
          _replicationTime = 0;
          _entityCache.WriteState(_writeBuffer);

          SendTimestamp();
        }

        _stats.Update(_connection.BytesSent, _connection.BytesReceived, _connection.Ping, dt);
      }

      listeners.Update();
      _connection.Update();
    }

    public void Dispose()
    {
      if (_status != RagonStatus.DISCONNECTED)
      {
        _status = RagonStatus.DISCONNECTED;
        _connection.Disconnect();
      }
      _connection.Dispose();
    }

    public void AddListener(IRagonListener listener) => listeners.Add(listener);
    public void AddListener(IRagonAuthorizationListener listener) => listeners.Add(listener);
    public void AddListener(IRagonConnectionListener listener) => listeners.Add(listener);
    public void AddListener(IRagonFailedListener listener) => listeners.Add(listener);
    public void AddListener(IRagonJoinListener listener) => listeners.Add(listener);
    public void AddListener(IRagonLeftListener listener) => listeners.Add(listener);
    public void AddListener(IRagonOwnershipChangedListener listener) => listeners.Add(listener);
    public void AddListener(IRagonPlayerJoinListener listener) => listeners.Add(listener);
    public void AddListener(IRagonPlayerLeftListener listener) => listeners.Add(listener);
    public void AddListener(IRagonSceneListener listener) => listeners.Add(listener);
    public void AddListener(IRagonSceneRequestListener listener) => listeners.Add(listener);
    public void AddListener(IRagonDataListener listener) => listeners.Add(listener);

    public void RemoveListener(IRagonListener listener) => listeners.Remove(listener);
    public void RemoveListener(IRagonAuthorizationListener listener) => listeners.Remove(listener);
    public void RemoveListener(IRagonConnectionListener listener) => listeners.Remove(listener);
    public void RemoveListener(IRagonFailedListener listener) => listeners.Remove(listener);
    public void RemoveListener(IRagonJoinListener listener) => listeners.Remove(listener);
    public void RemoveListener(IRagonLeftListener listener) => listeners.Remove(listener);
    public void RemoveListener(IRagonOwnershipChangedListener listener) => listeners.Remove(listener);
    public void RemoveListener(IRagonPlayerJoinListener listener) => listeners.Remove(listener);
    public void RemoveListener(IRagonPlayerLeftListener listener) => listeners.Remove(listener);
    public void RemoveListener(IRagonSceneListener listener) => listeners.Remove(listener);
    public void RemoveListener(IRagonSceneRequestListener listener) => listeners.Remove(listener);
    public void RemoveListener(IRagonDataListener listener) => listeners.Remove(listener);

    #endregion

    #region INTERNAL

    internal void AssignRoom(RagonRoom room)
    {
      _room?.Dispose();
      _room = room;
    }

    internal void SetStatus(RagonStatus status)
    {
      _status = status;
    }

    internal void SetTimestamp(double time)
    {
      _serverTimestamp = time;
    }

    #endregion

    #region PRIVATE

    private void SendTimestamp()
    {
      var timestamp = RagonTime.CurrentTimestamp();
      var value = new DoubleToUInt()
      {
        Double = timestamp,
      };

      _writeBuffer.Clear();
      _writeBuffer.WriteOperation(RagonOperation.TIMESTAMP_SYNCHRONIZATION);
      _writeBuffer.Write(value.Int0, 32);
      _writeBuffer.Write(value.Int1, 32);
    }

    private void OnConnected()
    {
      RagonLog.Trace("Connected");

      listeners.OnConnected();
      _status = RagonStatus.CONNECTED;
    }

    private void OnDisconnected(RagonDisconnect reason)
    {
      RagonLog.Trace($"Disconnected: {reason}");

      listeners.OnDisconnected(reason);
      _status = RagonStatus.DISCONNECTED;
    }

    private void OnData(byte[] data)
    {
      _readBuffer.Clear();
      _readBuffer.FromArray(data);

      var operation = _readBuffer.ReadByte();
      _handlers[operation].Handle(_readBuffer);
    }

    #endregion
  }
}