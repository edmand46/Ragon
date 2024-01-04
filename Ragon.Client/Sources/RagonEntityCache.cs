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

namespace Ragon.Client;

public sealed class RagonEntityCache
{
  private readonly List<RagonEntity> _entityList = new();
  private readonly Dictionary<uint, RagonEntity> _entityMap = new();
  private readonly Dictionary<uint, RagonEntity> _pendingEntities = new();
  private readonly Dictionary<uint, RagonEntity> _sceneEntities = new();

  private readonly RagonClient _client;
  private readonly IRagonSceneCollector _sceneCollector;
  private readonly RagonPlayerCache _playerCache;

  private int _localEntitiesCounter = 0;

  public RagonEntityCache(
    RagonClient client,
    RagonPlayerCache playerCache,
    IRagonSceneCollector sceneCollector
  )
  {
    _client = client;
    _sceneCollector = sceneCollector;
    _playerCache = playerCache;
  }

  public bool TryGetEntity(ushort id, out RagonEntity entity)
  {
    return _entityMap.TryGetValue(id, out entity);
  }

  public void Create(RagonEntity entity, RagonPayload spawnPayload)
  {
    var attachId = (ushort)(_playerCache.Local.PeerId + _localEntitiesCounter++);
    var buffer = _client.Buffer;

    buffer.Clear();
    buffer.WriteOperation(RagonOperation.CREATE_ENTITY);
    buffer.WriteUShort(attachId);
    buffer.WriteUShort(entity.Type);
    buffer.WriteByte((byte)entity.Authority);

    entity.State.WriteInfo(buffer);

    spawnPayload?.Write(buffer);

    _pendingEntities.Add(attachId, entity);

    var sendData = buffer.ToArray();
    _client.Reliable.Send(sendData);
  }

  public void Transfer(RagonEntity entity, RagonPlayer player)
  {
    var buffer = _client.Buffer;

    buffer.Clear();
    buffer.WriteOperation(RagonOperation.TRANSFER_ENTITY_OWNERSHIP);
    buffer.WriteUShort(entity.Id);
    buffer.WriteUShort(player.PeerId);

    var sendData = buffer.ToArray();
    _client.Reliable.Send(sendData);
  }

  public void Destroy(RagonEntity entity, RagonPayload destroyPayload)
  {
    if (!entity.IsAttached && !entity.HasAuthority)
    {
      RagonLog.Warn("Can't destroy object");
      return;
    }

    entity.SetReplication(false);

    var buffer = _client.Buffer;

    buffer.Clear();
    buffer.WriteOperation(RagonOperation.REMOVE_ENTITY);
    buffer.WriteUShort(entity.Id);

    destroyPayload?.Write(buffer);

    var sendData = buffer.ToArray();
    _client.Reliable.Send(sendData);
  }

  internal void WriteState(RagonBuffer buffer)
  {
    var changedEntities = 0u;

    buffer.Clear();
    buffer.WriteOperation(RagonOperation.REPLICATE_ENTITY_STATE);

    var offset = buffer.WriteOffset;
    buffer.Write(0, 16);

    foreach (var ent in _entityList)
    {
      if (!ent.IsAttached ||
          !ent.IsReplicated ||
          !ent.PropertiesChanged) continue;

      ent.Write(buffer);

      changedEntities++;
    }

    if (changedEntities <= 0) return;

    buffer.Write(changedEntities, 16, offset);

    var data = buffer.ToArray();
    _client.Unreliable.Send(data);
  }

  internal void WriteScene(RagonBuffer buffer)
  {
    _sceneEntities.Clear();

    var entities = _sceneCollector.Collect();
    buffer.WriteUShort((ushort)entities.Length);
    foreach (var entity in entities)
    {
      buffer.WriteUShort(entity.Type);
      buffer.WriteByte((byte)entity.Authority);
      buffer.WriteUShort(entity.SceneId);

      entity.State.WriteInfo(buffer);

      _sceneEntities.Add(entity.SceneId, entity);
    }
  }

  internal void CacheScene()
  {
    _sceneEntities.Clear();

    var entities = _sceneCollector.Collect();
    foreach (var entity in entities)
      _sceneEntities.Add(entity.SceneId, entity);
  }

  internal void Cleanup()
  {
    var payload = new RagonPayload(0);
    foreach (var ent in _entityList)
      ent.Detach(payload);

    _entityMap.Clear();
    _entityList.Clear();
  }

  internal RagonEntity TryGetEntity(ushort attachId, ushort entityType, ushort sceneId, ushort entityId, bool hasAuthority, out bool hasCreated)
  {
    if (sceneId > 0)
    {
      if (_sceneEntities.TryGetValue(sceneId, out var sceneEntity))
      {
        _entityMap.Add(entityId, sceneEntity);

        if (hasAuthority)
          _entityList.Add(sceneEntity);

        hasCreated = false;

        return sceneEntity;
      }
    }

    if (_pendingEntities.TryGetValue(attachId, out var pendingEntity) && hasAuthority)
    {
      _pendingEntities.Remove(attachId);
      _entityMap.Add(entityId, pendingEntity);
      _entityList.Add(pendingEntity);

      hasCreated = false;

      return pendingEntity;
    }

    var entity = new RagonEntity(entityType, sceneId);

    _entityMap.Add(entityId, entity);

    if (hasAuthority)
      _entityList.Add(entity);

    hasCreated = true;

    return entity;
  }


  internal void OnDestroy(ushort entityId, RagonPayload payload)
  {
    if (_entityMap.TryGetValue(entityId, out var entity))
    {
      _entityMap.Remove(entityId);
      _entityList.Remove(entity);

      entity.Detach(payload);
      entity.Dispose();
    }
  }

  internal void OnState(ushort entityId, RagonBuffer buffer)
  {
    if (_entityMap.TryGetValue(entityId, out var entity))
      entity.Read(buffer);
    else
      RagonLog.Warn($"Entity {entityId} not found!");
  }

  internal void OnEvent(RagonPlayer player, ushort entityId, ushort eventCode, RagonBuffer buffer)
  {
    if (_entityMap.TryGetValue(entityId, out var entity))
      entity.Event(eventCode, player, buffer);
    else
      RagonLog.Warn($"Entity {entityId} not found!");
  }

  internal void OnOwnershipChanged(RagonPlayer player, ushort entityId)
  {
    if (_entityMap.TryGetValue(entityId, out var entity))
    {
      if (player.IsLocal)
        _entityList.Add(entity);
      else
        _entityList.Remove(entity);

      entity.OnOwnershipChanged(player);
    }
    else
    {
      RagonLog.Warn($"Entity {entityId} not found!");
    }
  }
}