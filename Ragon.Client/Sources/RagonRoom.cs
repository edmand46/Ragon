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

namespace Ragon.Client
{
  public class RagonRoom : IDisposable
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
        _callbacks?.Remove(_callback);
        _localCallbacks?.Remove(_callback);

        _callbacks = null!;
        _localCallbacks = null!;
        _callback = null!;
      }
    }

    private delegate void OnEventDelegate(RagonPlayer player, RagonBuffer serializer);

    private readonly RagonClient _client;
    private readonly RagonScene _scene;
    private readonly RagonEntityCache _entityCache;
    private readonly RagonPlayerCache _playerCache;
    private readonly RoomParameters _parameters;
    private readonly RagonUserData _userData;

    public string Id => _parameters.RoomId;
    public int MinPlayers => _parameters.Min;
    public int MaxPlayers => _parameters.Max;
    public string Scene => _scene.Name;

    public IReadOnlyList<RagonPlayer> Players => _playerCache.Players;
    public RagonPlayer Local => _playerCache.Local;
    public RagonPlayer Owner => _playerCache.Owner;
    public RagonUserData UserData => _userData;

    private readonly Dictionary<int, OnEventDelegate> _events = new Dictionary<int, OnEventDelegate>();

    private readonly Dictionary<int, List<Action<RagonPlayer, IRagonEvent>>> _localListeners =
      new Dictionary<int, List<Action<RagonPlayer, IRagonEvent>>>();

    private readonly Dictionary<int, List<Action<RagonPlayer, IRagonEvent>>> _listeners =
      new Dictionary<int, List<Action<RagonPlayer, IRagonEvent>>>();

    public RagonRoom(RagonClient client,
      RagonEntityCache entityCache,
      RagonPlayerCache playerCache,
      RoomParameters parameters,
      RagonScene scene)
    {
      _client = client;
      _parameters = parameters;
      _entityCache = entityCache;
      _playerCache = playerCache;
      _scene = scene;
      _userData = new RagonUserData();
    }

    internal void Cleanup()
    {
      _entityCache.Cleanup();
      _playerCache.Cleanup();
    }

    internal void Update(string sceneName)
    {
      _scene.Update(sceneName);
    }

    internal void HandleEvent(ushort eventCode, RagonPlayer caller, RagonBuffer buffer)
    {
      if (_events.TryGetValue(eventCode, out var evnt))
        evnt?.Invoke(caller, buffer);
      else
        RagonLog.Warn($"Handler event on entity {Id} with eventCode {eventCode} not defined");
    }

    internal void HandleUserData(RagonBuffer buffer)
    {
      _userData.Read(buffer);
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

    public void LoadScene(string sceneName) => _scene.Load(sceneName);
    public void SceneLoaded() => _scene.SceneLoaded();

    public void ReplicateEvent<TEvent>(TEvent evnt, RagonTarget target, RagonReplicationMode mode)
      where TEvent : IRagonEvent, new() => _scene.ReplicateEvent(evnt, target, mode);

    public void ReplicateEvent<TEvent>(TEvent evnt, RagonPlayer target, RagonReplicationMode mode)
      where TEvent : IRagonEvent, new() => _scene.ReplicateEvent(evnt, target, mode);

    public void ReplicateData(byte[] data, bool reliable = false) => _scene.ReplicateData(data, reliable);

    public void CreateEntity(RagonEntity entity) => CreateEntity(entity, null);
    public void CreateEntity(RagonEntity entity, RagonPayload payload) => _entityCache.Create(entity, payload);
    public void TransferEntity(RagonEntity entity, RagonPlayer player) => _entityCache.Transfer(entity, player);

    public void DestroyEntity(RagonEntity entityId) => DestroyEntity(entityId, null);
    public void DestroyEntity(RagonEntity entityId, RagonPayload payload) => _entityCache.Destroy(entityId, payload);

    public void Dispose()
    {
      Cleanup();

      _events.Clear();
      _listeners.Clear();
      _localListeners.Clear();
    }
  }
}