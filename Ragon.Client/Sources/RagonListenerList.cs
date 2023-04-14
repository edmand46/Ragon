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
    private readonly List<IRagonLevelListener> _levelListeners = new();
    private readonly List<IRagonOwnershipChangedListener> _ownershipChangedListeners = new();
    private readonly List<IRagonPlayerJoinListener> _playerJoinListeners = new();
    private readonly List<IRagonPlayerLeftListener> _playerLeftListeners = new();
    
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
      _levelListeners.Add(listener);
      _ownershipChangedListeners.Add(listener);
      _playerJoinListeners.Add(listener);
      _playerLeftListeners.Add(listener);
    }

    public void Remove(IRagonListener listener)
    {
      _authorizationListeners.Remove(listener);
      _connectionListeners.Remove(listener);
      _failedListeners.Remove(listener);
      _joinListeners.Remove(listener);
      _leftListeners.Remove(listener);
      _levelListeners.Remove(listener);
      _ownershipChangedListeners.Remove(listener);
      _playerJoinListeners.Remove(listener);
      _playerLeftListeners.Remove(listener);
    }
    
    public void Add(IRagonAuthorizationListener listener)
    {
      _authorizationListeners.Add(listener);
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
    
    public void Add(IRagonLeftListener  listener)
    {
      _leftListeners.Add(listener);
    }
    
    public void Add(IRagonLevelListener  listener)
    {
      _levelListeners.Add(listener);
    }
    
    public void Add(IRagonOwnershipChangedListener  listener)
    {
      _ownershipChangedListeners.Add(listener);
    }

    public void Add(IRagonPlayerJoinListener  listener)
    {
      _playerJoinListeners.Add(listener);
    }
    
    public void Add(IRagonPlayerLeftListener  listener)
    {
      _playerLeftListeners.Add(listener);
    }

    public void Remove(IRagonAuthorizationListener listener)
    {
      _authorizationListeners.Remove(listener);
    }
    
    public void Remove(IRagonConnectionListener listener)
    {
      _connectionListeners.Remove(listener);
    }
    
    public void Remove(IRagonFailedListener listener)
    {
      _failedListeners.Remove(listener);
    }
    
    public void Remove(IRagonJoinListener listener)
    {
      _joinListeners.Remove(listener);
    }
    
    public void Remove(IRagonLeftListener  listener)
    {
      _leftListeners.Remove(listener);
    }
    
    public void Remove(IRagonLevelListener  listener)
    {
      _levelListeners.Remove(listener);
    }
    
    public void Remove(IRagonOwnershipChangedListener  listener)
    {
      _ownershipChangedListeners.Remove(listener);
    }

    public void Remove(IRagonPlayerJoinListener  listener)
    {
      _playerJoinListeners.Remove(listener);
    }
    
    public void Remove(IRagonPlayerLeftListener  listener)
    {
      _playerLeftListeners.Remove(listener);
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

    public void OnLevel(string sceneName)
    {
      foreach (var listener in _levelListeners)
        listener.OnLevel(_client, sceneName);
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

    public void OnDisconnected()
    {
      foreach (var listener in _connectionListeners)
        listener.OnDisconnected(_client);
    }
  }
}