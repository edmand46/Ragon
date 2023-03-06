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
  public class RagonRoom
  {
    private RagonClient _client;
    private RagonScene _scene;
    private RagonEntityCache _entityCache;
    private RagonPlayerCache _playerCache;
    private RagonRoomInformation _information;

    public string Id => _information.RoomId;
    public int MinPlayers => _information.Min;
    public int MaxPlayers => _information.Max;

    public RagonPlayer Local => _playerCache.LocalPlayer;
    public RagonPlayer Owner => _playerCache.Owner;

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

    public void LoadScene(string map) => _scene.Load(map);
    public void SceneLoaded() => _scene.SceneLoaded();

    public void CreateEntity(RagonEntity entity) => CreateEntity(entity, null);
    public void CreateEntity(RagonEntity entity, IRagonPayload? payload) => _entityCache.Create(entity, payload);

    public void DestroyEntity(RagonEntity entityId) => DestroyEntity(entityId, null);
    public void DestroyEntity(RagonEntity entityId, IRagonPayload? payload) => _entityCache.Destroy(entityId, payload);
  }
}