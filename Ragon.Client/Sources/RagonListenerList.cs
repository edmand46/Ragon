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
    public int Count => _listeners.Count;
    private List<IRagonListener> _listeners = new();
    private RagonClient _client;

    public RagonListenerList(RagonClient client)
    {
      _client = client;
    }
    
    public void Add(IRagonListener listener)
    {
      _listeners.Add(listener);
    }

    public void Remove(IRagonListener listener)
    {
      _listeners.Remove(listener);
    }
    
    public void OnAuthorizationSuccess(string playerId, string playerName)
    {
      foreach (var listener in _listeners)
        listener.OnAuthorizationSuccess(_client, playerId, playerName);
    }
    
    public void OnAuthorizationFailed(string message)
    {
      foreach (var listener in _listeners)
        listener.OnAuthorizationFailed(_client, message);
    }
    
    public void OnLeft()
    {
      foreach (var listener in _listeners)
        listener.OnLeft(_client);
    }

    public void OnFailed(string message)
    {
      foreach (var listener in _listeners)
        listener.OnFailed(_client, message);
    }

    public void OnOwnershipChanged(RagonPlayer player)
    {
      foreach (var listener in _listeners)
        listener.OnOwnershipChanged(_client, player);
    }

    public void OnPlayerLeft(RagonPlayer player)
    {
      foreach (var listener in _listeners)
        listener.OnPlayerLeft(_client, player);
    }

    public void OnPlayerJoined(RagonPlayer player)
    {
      foreach (var listener in _listeners)
        listener.OnPlayerJoined(_client, player);
    }

    public void OnLevel(string sceneName)
    {
      foreach (var listener in _listeners)
        listener.OnLevel(_client, sceneName);
    }

    public void OnJoined()
    {
      foreach (var listener in _listeners)
        listener.OnJoined(_client);
    }

    public void OnConnected()
    {
      foreach (var listener in _listeners)
        listener.OnConnected(_client);
    }

    public void OnDisconnected()
    {
      foreach (var listener in _listeners)
        listener.OnDisconnected(_client);
    }
  }
}