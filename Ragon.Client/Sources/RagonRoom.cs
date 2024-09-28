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

    private delegate void OnEventDelegate(RagonPlayer player, RagonStream serializer);

    private readonly RagonClient _client;
    private readonly RagonPlayerCache _playerCache;
    private readonly RoomParameters _parameters;
    private readonly RagonUserData _userData;

    public string Id => _parameters.RoomId;
    public int MinPlayers => _parameters.Min;
    public int MaxPlayers => _parameters.Max;
    public string Scene => "none";

    public IReadOnlyList<RagonPlayer> Players => _playerCache.Players;
    public RagonPlayer Local => _playerCache.Local;
    public RagonPlayer Owner => _playerCache.Owner;
    public RagonUserData UserData => _userData;

    private readonly Dictionary<int, OnEventDelegate> _events = new();

    private readonly Dictionary<int, List<Action<RagonPlayer, IRagonEvent>>> _localListeners = new();

    private readonly Dictionary<int, List<Action<RagonPlayer, IRagonEvent>>> _listeners = new();

    public RagonRoom(RagonClient client,
      RagonPlayerCache playerCache,
      RoomParameters parameters)
    {
      _client = client;
      _parameters = parameters;
      _playerCache = playerCache;
      _userData = new RagonUserData();
    }

    internal void Cleanup()
    {
      _playerCache.Cleanup();
    }

    internal void Update(string sceneName)
    {
      // _scene.Update(sceneName);
    }

    internal void HandleEvent(ushort eventCode, RagonPlayer caller, RagonStream buffer)
    {
      if (_events.TryGetValue(eventCode, out var evnt))
        evnt?.Invoke(caller, buffer);
      else
        RagonLog.Warn($"Handler event {Id} with eventCode {eventCode} not defined");
    }

    internal void HandleUserData(RagonStream buffer)
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

    public void ReplicateEvent<TEvent>(TEvent evnt, RagonTarget target, RagonReplicationMode replicationMode)
      where TEvent : IRagonEvent, new()
    {
      var evntId = _client.Event.GetEventCode(evnt);
      var buffer = _client.Buffer;

      {
        if (replicationMode == RagonReplicationMode.Local &&
            _localListeners.TryGetValue(evntId, out var localListeners))
        {
          foreach (var listener in localListeners)
            listener.Invoke(_client.Room.Local, evnt);
          return;
        }
      }

      {
        if (replicationMode == RagonReplicationMode.LocalAndServer &&
            _localListeners.TryGetValue(evntId, out var localListeners))
        {
          foreach (var listener in localListeners)
            listener.Invoke(_client.Room.Local, evnt);
        }
      }

      buffer.Clear();
      buffer.WriteOperation(RagonOperation.REPLICATE_ROOM_EVENT);
      buffer.WriteUShort(evntId);
      buffer.WriteByte((byte)replicationMode);
      buffer.WriteByte((byte)target);

      evnt.Serialize(buffer);

      var sendData = buffer.ToArray();
      _client.Reliable.Send(sendData);
    }

    public void ReplicateEvent<TEvent>(TEvent evnt, RagonPlayer target, RagonReplicationMode replicationMode)
      where TEvent : IRagonEvent, new()
    {
      var evntId = _client.Event.GetEventCode(evnt);
      var buffer = _client.Buffer;

      buffer.Clear();
      buffer.WriteOperation(RagonOperation.REPLICATE_ROOM_EVENT);
      buffer.WriteUShort(evntId);
      buffer.WriteByte((byte)replicationMode);
      buffer.WriteByte((byte)RagonTarget.Player);
      buffer.WriteUShort(target.PeerId);

      evnt.Serialize(buffer);

      var sendData = buffer.ToArray();
      _client.Reliable.Send(sendData);
    }

    public void ReplicateData(byte[] data, bool reliable)
    {
      var sendData = new byte[data.Length + 1];
      sendData[0] = (byte)RagonOperation.REPLICATE_RAW_DATA;
      Array.Copy(data, 0, sendData, 1, data.Length);

      if (reliable)
        _client.Reliable.Send(sendData);
      else
        _client.Unreliable.Send(sendData);
    }

    public void Dispose()
    {
      Cleanup();

      _events.Clear();
      _listeners.Clear();
      _localListeners.Clear();
    }
  }
}