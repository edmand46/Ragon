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
  public class RagonRoom
  {
    private delegate void OnEventDelegate(RagonPlayer player, RagonBuffer serializer);

    private RagonClient _client;
    private RagonScene _scene;
    private RagonEntityCache _entityCache;
    private RagonPlayerCache _playerCache;
    private RagonRoomInformation _information;

    public string Id => _information.RoomId;
    public int MinPlayers => _information.Min;
    public int MaxPlayers => _information.Max;
    public string Scene => _scene.Name;

    public IReadOnlyList<RagonPlayer> Players => _playerCache.Players;
    public RagonPlayer Local => _playerCache.Local;
    public RagonPlayer Owner => _playerCache.Owner;

    private readonly Dictionary<int, OnEventDelegate> _events = new Dictionary<int, OnEventDelegate>();
    private readonly Dictionary<int, List<Action<RagonPlayer, IRagonEvent>>> _localListeners = new Dictionary<int, List<Action<RagonPlayer, IRagonEvent>>>();
    private readonly Dictionary<int, List<Action<RagonPlayer, IRagonEvent>>> _listeners = new Dictionary<int, List<Action<RagonPlayer, IRagonEvent>>>();

    public RagonRoom(RagonClient client,
      RagonEntityCache entityCache,
      RagonPlayerCache playerCache,
      RagonRoomInformation information,
      RagonScene scene)
    {
      _client = client;
      _information = information;
      _entityCache = entityCache;
      _playerCache = playerCache;
      _scene = scene;
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

    internal void Event(ushort eventCode, RagonPlayer caller, RagonBuffer buffer)
    {
      if (_events.TryGetValue(eventCode, out var evnt))
        evnt?.Invoke(caller, buffer);
      else
        RagonLog.Warn($"Handler event on entity {Id} with eventCode {eventCode} not defined");
    }

    public Action<RagonPlayer, IRagonEvent> OnEvent<TEvent>(Action<RagonPlayer, TEvent> callback) where TEvent : IRagonEvent, new()
    {
      var t = new TEvent();
      var eventCode = _client.Event.GetEventCode(t);
      var callbacks = _listeners[eventCode];
      var action = (RagonPlayer player, IRagonEvent eventData) => callback.Invoke(player, (TEvent)eventData);
      
      if (callbacks == null)
      {
        callbacks = new List<Action<RagonPlayer, IRagonEvent>>();
        _listeners.Add(eventCode, callbacks);
      }

      var localCallbacks = _localListeners[eventCode];
      if (localCallbacks == null)
      {
        localCallbacks = new List<Action<RagonPlayer, IRagonEvent>>();
        _localListeners.Add(eventCode, callbacks);
      }
      
      callbacks.Add(action);
      localCallbacks.Add(action);

      _events.Add(eventCode, (player, serializer) =>
      {
        t.Deserialize(serializer);

        foreach (var callbackListener in callbacks)
          callbackListener.Invoke(player, t);
      });

      return action;
    }

    public void OffEvent<TEvent>(Action<RagonPlayer, IRagonEvent> callback) where TEvent : IRagonEvent, new()
    {
      var t = new TEvent();
      var eventCode = _client.Event.GetEventCode(t);
      var callbacks = _listeners[eventCode];
      var localCallbacks = _localListeners[eventCode];
      
      callbacks?.Remove(callback);
      localCallbacks?.Remove(callback);
    }

    public void LoadScene(string sceneName) => _scene.Load(sceneName);
    public void SceneLoaded() => _scene.SceneLoaded();

    public void ReplicateEvent<TEvent>(TEvent evnt, RagonTarget target, RagonReplicationMode mode) where TEvent : IRagonEvent, new() => _scene.ReplicateEvent(evnt, target, mode);
    public void ReplicateEvent<TEvent>(TEvent evnt, RagonPlayer target, RagonReplicationMode mode) where TEvent : IRagonEvent, new() => _scene.ReplicateEvent(evnt, target, mode);

    public void CreateEntity(RagonEntity entity) => CreateEntity(entity, null);
    public void CreateEntity(RagonEntity entity, RagonPayload payload) => _entityCache.Create(entity, payload);
    public void TransferEntity(RagonEntity entity, RagonPlayer player) => _entityCache.Transfer(entity, player);

    public void DestroyEntity(RagonEntity entityId) => DestroyEntity(entityId, null);
    public void DestroyEntity(RagonEntity entityId, RagonPayload payload) => _entityCache.Destroy(entityId, payload);
  }
}