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
  public sealed class RagonEntity : IDisposable
  {
    private class EventSubscription : IDisposable
    {
      private List<Action<RagonPlayer, IRagonEvent>> _callbacks;
      private List<Action<RagonPlayer, IRagonEvent>> _localCallbacks;
      private Action<RagonPlayer, IRagonEvent> _callback;

      public EventSubscription(
        List<Action<RagonPlayer, IRagonEvent>> callbacks,
        List<Action<RagonPlayer, IRagonEvent>> localCallbacks,
        Action<RagonPlayer, IRagonEvent> callback)
      {
        _callbacks = callbacks;
        _localCallbacks = localCallbacks;
        _callback = callback;
      }

      public void Dispose()
      {
        _callbacks.Remove(_callback);
        _localCallbacks.Remove(_callback);
      }
    }

    private delegate void OnEventDelegate(RagonPlayer player, RagonBuffer serializer);

    private RagonClient _client;

    public ushort Id { get; private set; }
    public ushort Type { get; private set; }
    public bool Replication { get; private set; }

    public RagonAuthority Authority { get; private set; }
    public RagonPlayer Owner { get; private set; }
    public RagonEntityState State { get; private set; }

    public bool IsAttached { get; private set; }
    public bool HasAuthority { get; private set; }

    public event Action<RagonEntity> Attached;
    public event Action Detached;
    public event Action<RagonPlayer, RagonPlayer> OwnershipChanged;

    internal bool PropertiesChanged => _propertiesChanged;
    internal ushort SceneId => _sceneId;

    private ushort _sceneId;
    private bool _propertiesChanged;

    private RagonPayload _spawnPayload;
    private RagonPayload _destroyPayload;

    private readonly Dictionary<int, OnEventDelegate> _events = new Dictionary<int, OnEventDelegate>();
    private readonly Dictionary<int, List<Action<RagonPlayer, IRagonEvent>>> _localListeners = new Dictionary<int, List<Action<RagonPlayer, IRagonEvent>>>();
    private readonly Dictionary<int, List<Action<RagonPlayer, IRagonEvent>>> _listeners = new Dictionary<int, List<Action<RagonPlayer, IRagonEvent>>>();

    public RagonEntity(ushort type = 0, ushort sceneId = 0)
    {
      State = new RagonEntityState(this);
      Type = type;
      
      _spawnPayload = new RagonPayload(0);
      _destroyPayload = new RagonPayload(0);
      _sceneId = sceneId;
    }

    internal void Attach(RagonClient client, ushort entityId, ushort entityType, bool hasAuthority, RagonPlayer owner)
    {
      Type = entityType;
      Id = entityId;
      Owner = owner;
      IsAttached = true;
      Replication = true;
      HasAuthority = hasAuthority;

      _client = client;

      Attached?.Invoke(this);
    }

    internal void SetReplication(bool enabled)
    {
      Replication = enabled;
    }
    
    internal void Detach(RagonPayload payload)
    {
      _destroyPayload = payload;

      Detached?.Invoke();
    }

    internal T GetPayload<T>(RagonPayload data) where T : IRagonPayload, new()
    {
      var payload = new T();
      if (data.Size <= 0) return payload;
      
      var buffer = new RagonBuffer();
      
      data.Write(buffer);
      
      payload.Deserialize(buffer);
      
      return payload;
    }
    
    public void AttachPayload(RagonPayload payload)
    {
      _spawnPayload = payload;
    } 

    public T GetAttachPayload<T>() where T : IRagonPayload, new()
    {
      return GetPayload<T>(_spawnPayload);
    }

    public T GetDetachPayload<T>() where T : IRagonPayload, new()
    {
      return GetPayload<T>(_destroyPayload);
    }

    public void ReplicateEvent<TEvent>(TEvent evnt, RagonPlayer target, RagonReplicationMode replicationMode)
      where TEvent : IRagonEvent, new()
    {
      if (!IsAttached)
      {
        RagonLog.Error("Entity not attached");
        return;
      }

      var evntId = _client.Event.GetEventCode(evnt);
      var buffer = _client.Buffer;

      buffer.Clear();
      buffer.WriteOperation(RagonOperation.REPLICATE_ENTITY_EVENT);
      buffer.WriteUShort(Id);
      buffer.WriteUShort(evntId);
      buffer.WriteByte((byte)replicationMode);
      buffer.WriteByte((byte)RagonTarget.Player);
      buffer.WriteUShort(target.PeerId);

      evnt.Serialize(buffer);

      var sendData = buffer.ToArray();
      _client.Reliable.Send(sendData);
    }

    public void ReplicateEvent<TEvent>(
      TEvent evnt,
      RagonTarget target = RagonTarget.All,
      RagonReplicationMode replicationMode = RagonReplicationMode.Server)
      where TEvent : IRagonEvent, new()
    {
      if (!IsAttached)
      {
        RagonLog.Error("Entity not attached");
        return;
      }

      var eventCode = _client.Event.GetEventCode(evnt);
      if (target != RagonTarget.ExceptOwner)
      {
        if (replicationMode == RagonReplicationMode.Local)
        {
          var localListeners = _localListeners[eventCode];
          foreach (var listener in localListeners)
            listener.Invoke(_client.Room.Local, evnt);
          return;
        }
        if (replicationMode == RagonReplicationMode.LocalAndServer)
        {
          var localListeners = _localListeners[eventCode];
          foreach (var listener in localListeners)
            listener.Invoke(_client.Room.Local, evnt);
        }
      }

      var buffer = _client.Buffer;
      
      buffer.Clear();
      buffer.WriteOperation(RagonOperation.REPLICATE_ENTITY_EVENT);
      buffer.WriteUShort(Id);
      buffer.WriteUShort(eventCode);
      buffer.WriteByte((byte)replicationMode);
      buffer.WriteByte((byte)target);

      evnt.Serialize(buffer);

      var sendData = buffer.ToArray();
      _client.Reliable.Send(sendData);
    }

    public IDisposable OnEvent<TEvent>(Action<RagonPlayer, TEvent> callback) where TEvent : IRagonEvent, new()
    {
      var t = new TEvent();
      var eventCode = _client.Event.GetEventCode(t);
      var action = (RagonPlayer player, IRagonEvent eventData) => callback.Invoke(player, (TEvent)eventData);

      if (!_listeners.TryGetValue(eventCode, out var callbacks))
      {
        callbacks = new List<Action<RagonPlayer, IRagonEvent>>();
        _listeners.Add(eventCode, callbacks);
      }

      if (!_localListeners.TryGetValue(eventCode, out var localCallbacks))
      {
        localCallbacks = new List<Action<RagonPlayer, IRagonEvent>>();
        _localListeners.Add(eventCode, localCallbacks);
      }

      callbacks.Add(action);
      localCallbacks.Add(action);

      if (!_events.ContainsKey(eventCode))
      {
        _events.Add(eventCode, (player, serializer) =>
        {
          t.Deserialize(serializer);

          foreach (var callbackListener in callbacks)
            callbackListener.Invoke(player, t);
        });
      }

      return new EventSubscription(callbacks, localCallbacks, action);
    }

    internal void Write(RagonBuffer buffer)
    {
      buffer.WriteUShort(Id);

      State.WriteState(buffer);

      _propertiesChanged = false;
    }


    internal void Read(RagonBuffer buffer)
    {
      State.ReadState(buffer);
    }

    internal void Event(ushort eventCode, RagonPlayer caller, RagonBuffer buffer)
    {
      if (_events.TryGetValue(eventCode, out var evnt))
        evnt?.Invoke(caller, buffer);
      else
        RagonLog.Warn($"Handler event on entity {Id} with eventCode {eventCode} not defined");
    }

    internal void TrackChangedProperty(RagonProperty property)
    {
      _propertiesChanged = true;
    }

    public void OnOwnershipChanged(RagonPlayer player)
    {
      var prevOwner = Owner;

      Owner = player;
      HasAuthority = player.IsLocal;

      OwnershipChanged?.Invoke(prevOwner, player);
    }

    public void Dispose()
    {
      _events.Clear();
      _listeners.Clear();
      _localListeners.Clear();
    }
  }
}