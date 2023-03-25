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
    private readonly IRagonEntityListener _entityListener;
    private readonly IRagonSceneCollector _sceneCollector;
    private Handler[] _handlers;
    private RagonBuffer _readBuffer;
    private RagonBuffer _writeBuffer;
    private RagonRoom _room;
    private RagonSession _session;
    private RagonListenerList _listenerList;
    private RagonPlayerCache _playerCache;
    private RagonEntityCache _entityCache;
    private RagonEventCache _eventCache;
    private RagonStatus _status;
    private NetworkStatistics _stats;

    private float _replicationRate = 0;
    private float _replicationTime = 0;

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

    public RagonClient(
      INetworkConnection connection, 
      IRagonEntityListener entityListener,
      IRagonSceneCollector sceneCollector, 
      int rate)
    {
      _listenerList = new RagonListenerList(this);
      _entityListener = entityListener;
      _sceneCollector = sceneCollector;
      
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
    
    public void Connect(string address, ushort port, string protocol)
    {
      _writeBuffer = new RagonBuffer();
      _readBuffer = new RagonBuffer();
      _session = new RagonSession(this, _readBuffer);

      _playerCache = new RagonPlayerCache();
      _entityCache = new RagonEntityCache(this, _playerCache, _entityListener, _sceneCollector);

      _handlers = new Handler[byte.MaxValue];
      _handlers[(byte)RagonOperation.AUTHORIZED_SUCCESS] = new AuthorizeSuccessHandler(_listenerList);
      _handlers[(byte)RagonOperation.AUTHORIZED_FAILED] = new AuthorizeFailedHandler(_listenerList);
      _handlers[(byte)RagonOperation.JOIN_SUCCESS] =
        new JoinSuccessHandler(this, _readBuffer, _listenerList, _playerCache, _entityCache);
      _handlers[(byte)RagonOperation.JOIN_FAILED] = new JoinFailedHandler(_listenerList);
      _handlers[(byte)RagonOperation.LEAVE_ROOM] = new LeaveRoomHandler(this, _listenerList, _entityCache);
      _handlers[(byte)RagonOperation.OWNERSHIP_CHANGED] =
        new OwnershipHandler(_listenerList, _playerCache, _entityCache);
      _handlers[(byte)RagonOperation.PLAYER_JOINED] = new PlayerJoinHandler(_playerCache, _listenerList);
      _handlers[(byte)RagonOperation.PLAYER_LEAVED] =
        new PlayerLeftHandler(_entityCache, _playerCache, _listenerList);
      _handlers[(byte)RagonOperation.LOAD_SCENE] = new SceneLoadHandler(this, _listenerList);
      _handlers[(byte)RagonOperation.CREATE_ENTITY] = new EntityCreateHandler(this, _playerCache, _entityCache);
      _handlers[(byte)RagonOperation.DESTROY_ENTITY] = new EntityDestroyHandler(_entityCache);
      _handlers[(byte)RagonOperation.REPLICATE_ENTITY_STATE] = new StateEntityHandler(_entityCache);
      _handlers[(byte)RagonOperation.REPLICATE_ENTITY_EVENT] =
        new EntityEventHandler(this, _playerCache, _entityCache);
      _handlers[(byte)RagonOperation.SNAPSHOT] =
        new SnapshotHandler(this, _listenerList, _entityCache, _playerCache);

      var protocolRaw = RagonVersion.Parse(protocol);
      _connection.Connect(address, port, protocolRaw);
    }

    public void Disconnect()
    {
      _status = RagonStatus.DISCONNECTED;
      _room.Cleanup();
      _connection.Disconnect();
      OnDisconnected(DisconnectReason.MANUAL);
    }

    public void Update(float dt)
    {
      _replicationTime += dt;
      if (_replicationTime >= _replicationRate)
      {
        _entityCache.WriteState(_readBuffer);
        _replicationTime = 0;
      }

      _stats.Update(_connection.BytesSent, _connection.BytesReceived, _connection.Ping, dt);
      _connection.Update();
    }

    public void Dispose()
    {
      _status = RagonStatus.DISCONNECTED;
      _connection.Disconnect();
      _connection.Dispose();
    }

    public void AddListener(IRagonListener listener) => _listenerList.Add(listener);
    public void AddListener(IRagonAuthorizationListener listener) => _listenerList.Add(listener);
    public void AddListener(IRagonConnectedListener listener) => _listenerList.Add(listener);
    public void AddListener(IRagonFailedListener listener) => _listenerList.Add(listener);
    public void AddListener(IRagonJoinListener listener) => _listenerList.Add(listener);
    public void AddListener(IRagonLeftListener listener) => _listenerList.Add(listener);
    public void AddListener(IRagonLevelListener listener) => _listenerList.Add(listener);
    public void AddListener(IRagonOwnershipChangedListener listener) => _listenerList.Add(listener);
    public void AddListener(IRagonPlayerJoinListener listener) => _listenerList.Add(listener);
    public void AddListener(IRagonPlayerLeftListener listener) => _listenerList.Add(listener);

    public void RemoveListener(IRagonListener listener) => _listenerList.Remove(listener);
    public void RemoveListener(IRagonAuthorizationListener listener) => _listenerList.Remove(listener);
    public void RemoveListener(IRagonConnectedListener listener) => _listenerList.Remove(listener);
    public void RemoveListener(IRagonFailedListener listener) => _listenerList.Remove(listener);
    public void RemoveListener(IRagonJoinListener listener) => _listenerList.Remove(listener);
    public void RemoveListener(IRagonLeftListener listener) => _listenerList.Remove(listener);
    public void RemoveListener(IRagonLevelListener listener) => _listenerList.Remove(listener);
    public void RemoveListener(IRagonOwnershipChangedListener listener) => _listenerList.Remove(listener);
    public void RemoveListener(IRagonPlayerJoinListener listener) => _listenerList.Remove(listener);
    public void RemoveListener(IRagonPlayerLeftListener listener) => _listenerList.Remove(listener);
    
    #endregion

    #region INTERNAL

    internal void AssignRoom(RagonRoom room)
    {
      _room = room;
      _status = RagonStatus.ROOM;
    }

    #endregion

    #region PRIVATE

    private void OnConnected()
    {
      RagonLog.Trace("Connected");

      _listenerList.OnConnected();
      _status = RagonStatus.CONNECTED;
    }

    private void OnDisconnected(DisconnectReason reason)
    {
      RagonLog.Trace($"Disconnected: {reason}");

      _listenerList.OnDisconnected();
      _status = RagonStatus.DISCONNECTED;
    }

    public void OnData(byte[] data)
    {
      _readBuffer.Clear();
      _readBuffer.FromArray(data);

      var operation = _readBuffer.ReadByte();
      _handlers[operation].Handle(_readBuffer);
    }

    #endregion
  }
}