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
  internal class RagonListenerList
  {
    private readonly RagonClient _client;
    private readonly List<IRagonAuthorizationListener> _authorizationListeners = new();
    private readonly List<IRagonConnectionListener> _connectionListeners = new();
    private readonly List<IRagonFailedListener> _failedListeners = new();
    private readonly List<IRagonJoinListener> _joinListeners = new();
    private readonly List<IRagonLeftListener> _leftListeners = new();
    private readonly List<IRagonSceneListener> _sceneListeners = new();
    private readonly List<IRagonSceneRequestListener> _sceneRequestListeners = new();
    private readonly List<IRagonOwnershipChangedListener> _ownershipChangedListeners = new();
    private readonly List<IRagonPlayerJoinListener> _playerJoinListeners = new();
    private readonly List<IRagonPlayerLeftListener> _playerLeftListeners = new();
    private readonly List<IRagonDataListener> _dataListeners = new();
    private readonly List<Action> _delayedActions = new();

    public RagonListenerList(RagonClient client)
    {
      _client = client;
    }

    public void Add(IRagonListener listener)
    {
      _authorizationListeners.Add(listener);
      _connectionListeners.Add(listener);
      _failedListeners.Add(listener);
      _joinListeners.Add(listener);
      _leftListeners.Add(listener);
      _sceneListeners.Add(listener);
      _ownershipChangedListeners.Add(listener);
      _playerJoinListeners.Add(listener);
      _playerLeftListeners.Add(listener);
    }

    public void Remove(IRagonListener listener)
    {
      _delayedActions.Add(() =>
      {
        _authorizationListeners.Remove(listener);
        _connectionListeners.Remove(listener);
        _failedListeners.Remove(listener);
        _joinListeners.Remove(listener);
        _leftListeners.Remove(listener);
        _sceneListeners.Remove(listener);
        _ownershipChangedListeners.Remove(listener);
        _playerJoinListeners.Remove(listener);
        _playerLeftListeners.Remove(listener);
      });
    }

    public void Update()
    {
      foreach (var action in _delayedActions)
        action.Invoke();

      _delayedActions.Clear();
    }

    public void Add(IRagonDataListener dataListener)
    {
      _dataListeners.Add(dataListener);
    }
    
    public void Add(IRagonAuthorizationListener listener)
    {
      _authorizationListeners.Add(listener);
    }

    public void Add(IRagonSceneRequestListener listener)
    {
      _sceneRequestListeners.Add(listener);
    }

    public void Add(IRagonConnectionListener listener)
    {
      _connectionListeners.Add(listener);
    }

    public void Add(IRagonFailedListener listener)
    {
      _failedListeners.Add(listener);
    }

    public void Add(IRagonJoinListener listener)
    {
      _joinListeners.Add(listener);
    }

    public void Add(IRagonLeftListener listener)
    {
      _leftListeners.Add(listener);
    }

    public void Add(IRagonSceneListener listener)
    {
      _sceneListeners.Add(listener);
    }

    public void Add(IRagonOwnershipChangedListener listener)
    {
      _ownershipChangedListeners.Add(listener);
    }

    public void Add(IRagonPlayerJoinListener listener)
    {
      _playerJoinListeners.Add(listener);
    }

    public void Add(IRagonPlayerLeftListener listener)
    {
      _playerLeftListeners.Add(listener);
    }
    
    public void Remove(IRagonDataListener listener)
    {
      _delayedActions.Add(() => _dataListeners.Remove(listener));
    }
    
    public void Remove(IRagonSceneRequestListener listener)
    {
      _delayedActions.Add(() => _sceneRequestListeners.Remove(listener));
    }

    public void Remove(IRagonAuthorizationListener listener)
    {
      _delayedActions.Add(() => _authorizationListeners.Remove(listener));
    }

    public void Remove(IRagonConnectionListener listener)
    {
      _delayedActions.Add(() => _connectionListeners.Remove(listener));
    }

    public void Remove(IRagonFailedListener listener)
    {
      _delayedActions.Add(() => _failedListeners.Remove(listener));
    }

    public void Remove(IRagonJoinListener listener)
    {
      _delayedActions.Add(() => _joinListeners.Remove(listener));
    }

    public void Remove(IRagonLeftListener listener)
    {
      _delayedActions.Add(() => _leftListeners.Remove(listener));
    }

    public void Remove(IRagonSceneListener listener)
    {
      _delayedActions.Add(() => _sceneListeners.Remove(listener));
    }

    public void Remove(IRagonOwnershipChangedListener listener)
    {
      _delayedActions.Add(() => _ownershipChangedListeners.Remove(listener));
    }

    public void Remove(IRagonPlayerJoinListener listener)
    {
      _delayedActions.Add(() => _playerJoinListeners.Remove(listener));
    }

    public void Remove(IRagonPlayerLeftListener listener)
    {
      _delayedActions.Add(() => _playerLeftListeners.Remove(listener));
    }

    public void OnAuthorizationSuccess(string playerId, string playerName, string payload)
    {
      foreach (var listener in _authorizationListeners)
        listener.OnAuthorizationSuccess(_client, playerId, playerName);
    }

    public void OnAuthorizationFailed(string message)
    {
      foreach (var listener in _authorizationListeners)
        listener.OnAuthorizationFailed(_client, message);
    }

    public void OnLeft()
    {
      foreach (var listener in _leftListeners)
        listener.OnLeft(_client);
    }

    public void OnFailed(string message)
    {
      foreach (var listener in _failedListeners)
        listener.OnFailed(_client, message);
    }

    public void OnOwnershipChanged(RagonPlayer player)
    {
      foreach (var listener in _ownershipChangedListeners)
        listener.OnOwnershipChanged(_client, player);
    }

    public void OnPlayerLeft(RagonPlayer player)
    {
      foreach (var listener in _playerLeftListeners)
        listener.OnPlayerLeft(_client, player);
    }

    public void OnPlayerJoined(RagonPlayer player)
    {
      foreach (var listener in _playerJoinListeners)
        listener.OnPlayerJoined(_client, player);
    }

    public void OnSceneLoaded()
    {
      foreach (var listener in _sceneListeners)
        listener.OnSceneLoaded(_client);
    }

    public void OnSceneRequest(string sceneName)
    {
      foreach (var listener in _sceneRequestListeners)
        listener.OnRequestScene(_client, sceneName);
    }

    public void OnJoined()
    {
      foreach (var listener in _joinListeners)
        listener.OnJoined(_client);
    }

    public void OnConnected()
    {
      foreach (var listener in _connectionListeners)
        listener.OnConnected(_client);
    }

    public void OnDisconnected(RagonDisconnect disconnect)
    {
      foreach (var listener in _connectionListeners)
        listener.OnDisconnected(_client, disconnect);
    }

    public void OnData(RagonPlayer player, byte[] data)
    {
      foreach (var listener in _dataListeners)
        listener.OnData(player, data);
    }
  }
}