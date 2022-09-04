using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using Ragon.Common;

namespace Ragon.Core
{
  public class GameRoom : IGameRoom
  {
    public int PlayersMin { get; private set; }
    public int PlayersMax { get; private set; }
    public int PlayersCount => _players.Count;
    public int EntitiesCount => _entities.Count;
    public string Id { get; private set; }
    public string Map { get; private set; }

    private ILogger _logger = LogManager.GetCurrentClassLogger();
    private Dictionary<uint, Player> _players = new();
    private Dictionary<int, Entity> _entities = new();
    private uint _owner;

    private readonly IScheduler _scheduler;
    private readonly IGameThread _gameThread;
    private readonly PluginBase _plugin;
    private readonly RagonSerializer _serializer = new(512);

    // Cache
    private uint[] _readyPlayers = Array.Empty<uint>();
    private uint[] _allPlayers = Array.Empty<uint>();
    private Entity[] _entitiesAll = Array.Empty<Entity>();
    private HashSet<Entity> _entitiesDirtySet = new HashSet<Entity>();
    private List<Entity> _entitiesDirty = new List<Entity>();
    private List<uint> _peersCache = new List<uint>();

    public GameRoom(IGameThread gameThread, PluginBase pluginBase, string roomId, string map, int min, int max)
    {
      _gameThread = gameThread;
      _plugin = pluginBase;
      _scheduler = new Scheduler();

      Map = map;
      PlayersMin = min;
      PlayersMax = max;
      Id = roomId;

      _plugin.Attach(this);
    }

    public void AddPlayer(Player player, ReadOnlySpan<byte> payload)
    {
      if (_players.Count == 0)
      {
        _owner = player.PeerId;
      }

      _players.Add(player.PeerId, player);
      _allPlayers = _players.Select(p => p.Key).ToArray();

      {
        _serializer.Clear();
        _serializer.WriteOperation(RagonOperation.JOIN_SUCCESS);
        _serializer.WriteString(Id);
        _serializer.WriteString(player.Id);
        _serializer.WriteString(GetOwner().Id);
        _serializer.WriteUShort((ushort) PlayersMin);
        _serializer.WriteUShort((ushort) PlayersMax);

        var sendData = _serializer.ToArray();
        Send(player.PeerId, sendData, DeliveryType.Reliable);
      }

      {
        _serializer.Clear();
        _serializer.WriteOperation(RagonOperation.LOAD_SCENE);
        _serializer.WriteString(Map);

        var sendData = _serializer.ToArray();
        Send(player.PeerId, sendData, DeliveryType.Reliable);
      }
    }

    public void RemovePlayer(uint peerId)
    {
      if (_players.Remove(peerId, out var player))
      {
        _allPlayers = _players.Select(p => p.Key).ToArray();
        _readyPlayers = _players.Where(p => p.Value.IsLoaded).Select(p => p.Key).ToArray();

        {
          _plugin.OnPlayerLeaved(player);

          _serializer.Clear();
          _serializer.WriteOperation(RagonOperation.PLAYER_LEAVED);
          _serializer.WriteString(player.Id);

          // TODO: DO NOT REMOVE STATIC ENTITIES
          _serializer.WriteUShort((ushort) player.EntitiesIds.Count);
          foreach (var entityId in player.EntitiesIds)
          {
            _serializer.WriteUShort(entityId);
            _entities.Remove(entityId);
          }

          var sendData = _serializer.ToArray();
          Broadcast(_readyPlayers, sendData);
        }

        if (_allPlayers.Length > 0 && player.PeerId == _owner)
        {
          var nextOwnerId = _allPlayers[0];
          _owner = nextOwnerId;
          var nextOwner = _players[nextOwnerId];

          _serializer.Clear();
          _serializer.WriteOperation(RagonOperation.OWNERSHIP_CHANGED);
          _serializer.WriteString(nextOwner.Id);

          var sendData = _serializer.ToArray();
          Broadcast(_readyPlayers, sendData);
        }

        _entitiesAll = _entities.Values.ToArray();
      }
    }

    public void ProcessEvent(ushort peerId, RagonOperation operation, ReadOnlySpan<byte> payloadRawData)
    {
      _serializer.Clear();
      _serializer.FromSpan(ref payloadRawData);
      switch (operation)
      {
        case RagonOperation.LOAD_SCENE:
        {
          var sceneName = _serializer.ReadString();
          _readyPlayers = Array.Empty<uint>();
          _entitiesAll = Array.Empty<Entity>();
          _entities.Clear();

          _serializer.Clear();
          _serializer.WriteOperation(RagonOperation.LOAD_SCENE);
          _serializer.WriteString(sceneName);

          var sendData = _serializer.ToArray();
          Broadcast(_allPlayers, sendData, DeliveryType.Reliable);
          break;
        }
        case RagonOperation.SCENE_LOADED:
        {
          var player = _players[peerId];
          
          if (peerId == _owner)
          {
            var statics = _serializer.ReadUShort();
            for (var staticIndex = 0; staticIndex < statics; staticIndex++)
            {
              var entityType = _serializer.ReadUShort();
              var staticId = _serializer.ReadUShort();

              var propertiesCount = _serializer.ReadUShort();
              var entity = new Entity(peerId, entityType, staticId, RagonAuthority.ALL, RagonAuthority.OWNER_ONLY, propertiesCount);
              for (var propertyIndex = 0; propertyIndex < propertiesCount; propertyIndex++)
              {
                var propertyType = _serializer.ReadBool();
                var propertySize = _serializer.ReadUShort();
                entity.Properties[propertyIndex] = new EntityProperty(propertySize, propertyType);
              }
              
              player.Entities.Add(entity);
              player.EntitiesIds.Add(entity.EntityId);
              
              _entities.Add(entity.EntityId, entity);
            }
            
            _entitiesAll = _entities.Values.ToArray();
            _logger.Trace($"Scene entities: {statics}");
          }

          ReplicateSnapshot(peerId);
          
          _serializer.Clear();
          _serializer.WriteOperation(RagonOperation.PLAYER_JOINED);
          _serializer.WriteUShort((ushort) player.PeerId);
          _serializer.WriteString(player.Id);
          _serializer.WriteString(player.PlayerName);

          var sendData = _serializer.ToArray();
          var readyPlayersWithExcludedPeer = _readyPlayers.Where(p => p != peerId).ToArray();
          Broadcast(readyPlayersWithExcludedPeer, sendData, DeliveryType.Reliable);
          
          player.IsLoaded = true;
          _readyPlayers = _players.Where(p => p.Value.IsLoaded).Select(p => p.Key).ToArray();
          _plugin.OnPlayerJoined(player);
          break;
        }
        case RagonOperation.REPLICATE_ENTITY_STATE:
        {
          var entitiesCount = _serializer.ReadUShort();
          for (var entityIndex = 0; entityIndex < entitiesCount; entityIndex++)
          {
            var entityId = _serializer.ReadUShort();
            if (_entities.TryGetValue(entityId, out var ent))
            {
              if (ent.OwnerId != peerId)
              {
                _logger.Warn($"Not owner can't change properties of object {entityId}");
                return;
              }

              for (var i = 0; i < ent.Properties.Length; i++)
              {
                if (_serializer.ReadBool())
                {
                  var property = ent.Properties[i];
                  var size = property.Size;
                  if (!property.IsFixed)
                    size = _serializer.ReadUShort();

                  if (size > property.Capacity)
                  {
                    _logger.Warn($"Property {i} payload too large, size: {size}");
                    continue;
                  }

                  var propertyPayload = _serializer.ReadData(size);
                  property.Write(ref propertyPayload);
                  property.Size = size;
                }
              }

              if (_entitiesDirtySet.Add(ent))
                _entitiesDirty.Add(ent);
            }
            else
            {
              _logger.Error($"Entity with Id {entityId} not found, replication interrupted");
              break;
            }
          }

          break;
        }
        case RagonOperation.REPLICATE_ENTITY_EVENT:
        {
          var evntId = _serializer.ReadUShort();
          var evntMode = _serializer.ReadByte();
          var targetMode = (RagonTarget) _serializer.ReadByte();
          var entityId = _serializer.ReadUShort();

          if (!_entities.TryGetValue(entityId, out var ent))
            return;

          if (ent.Authority == RagonAuthority.OWNER_ONLY && ent.OwnerId != peerId)
            return;

          Span<byte> payloadRaw = stackalloc byte[_serializer.Size];
          var payloadData = _serializer.ReadData(_serializer.Size);
          payloadData.CopyTo(payloadRaw);

          ReadOnlySpan<byte> payload = payloadRaw;
          if (_plugin.InternalHandle(peerId, entityId, evntId, ref payload))
            return;

          _serializer.Clear();
          _serializer.WriteOperation(RagonOperation.REPLICATE_ENTITY_EVENT);
          _serializer.WriteUShort(evntId);
          _serializer.WriteUShort(peerId);
          _serializer.WriteByte(evntMode);
          _serializer.WriteUShort(entityId);
          _serializer.WriteData(ref payload);

          var sendData = _serializer.ToArray();

          switch (targetMode)
          {
            case RagonTarget.OWNER:
            {
              Send(ent.OwnerId, sendData, DeliveryType.Reliable);
              break;
            }
            case RagonTarget.EXCEPT_OWNER:
            {
              _peersCache.Clear();
              foreach (var playerPeerId in _readyPlayers)
                if (playerPeerId != ent.OwnerId)
                  _peersCache.Add(playerPeerId);

              Broadcast(_peersCache.ToArray(), sendData, DeliveryType.Reliable);
              break;
            }
            case RagonTarget.ALL:
            {
              Broadcast(_readyPlayers, sendData, DeliveryType.Reliable);
              break;
            }
          }

          break;
        }
        case RagonOperation.CREATE_ENTITY:
        {
          var entityType = _serializer.ReadUShort();
          var propertiesCount = _serializer.ReadUShort();
          var entity = new Entity(peerId, entityType, 0, RagonAuthority.ALL, RagonAuthority.ALL, propertiesCount);
          for (var i = 0; i < propertiesCount; i++)
          {
            var propertyType = _serializer.ReadBool();
            var propertySize = _serializer.ReadUShort();
            entity.Properties[i] = new EntityProperty(propertySize, propertyType);
          }

          {
            var entityPayload = _serializer.ReadData(_serializer.Size);
            entity.Payload = entityPayload.ToArray();
          }

          var player = _players[peerId];
          player.Entities.Add(entity);
          player.EntitiesIds.Add(entity.EntityId);

          var ownerId = peerId;

          _entities.Add(entity.EntityId, entity);
          _entitiesAll = _entities.Values.ToArray();

          _plugin.OnEntityCreated(player, entity);

          _serializer.Clear();
          _serializer.WriteOperation(RagonOperation.CREATE_ENTITY);
          _serializer.WriteUShort(entityType);
          _serializer.WriteUShort(entity.EntityId);
          _serializer.WriteUShort(ownerId);

          {
            ReadOnlySpan<byte> entityPayload = entity.Payload.AsSpan();
            _serializer.WriteUShort((ushort) entityPayload.Length);
            _serializer.WriteData(ref entityPayload);
          }

          var sendData = _serializer.ToArray();
          Broadcast(_readyPlayers, sendData, DeliveryType.Reliable);
          break;
        }
        case RagonOperation.DESTROY_ENTITY:
        {
          var entityId = _serializer.ReadInt();
          if (_entities.TryGetValue(entityId, out var entity))
          {
            if (entity.Authority == RagonAuthority.OWNER_ONLY && entity.OwnerId != peerId)
              return;

            var player = _players[peerId];
            var destroyPayload = _serializer.ReadData(_serializer.Size);

            player.Entities.Remove(entity);
            player.EntitiesIds.Remove(entity.EntityId);

            _entities.Remove(entityId);
            _entitiesAll = _entities.Values.ToArray();

            _plugin.OnEntityDestroyed(player, entity);

            _serializer.Clear();
            _serializer.WriteOperation(RagonOperation.DESTROY_ENTITY);
            _serializer.WriteInt(entityId);
            _serializer.WriteUShort((ushort) destroyPayload.Length);
            _serializer.WriteData(ref destroyPayload);

            var sendData = _serializer.ToArray();
            Broadcast(_readyPlayers, sendData, DeliveryType.Reliable);
          }

          break;
        }
      }
    }

    public void Tick(float deltaTime)
    {
      _scheduler.Tick(deltaTime);

      ReplicateProperties();
    }

    void ReplicateSnapshot(uint peerId)
    {
      _serializer.Clear();
      _serializer.WriteOperation(RagonOperation.SNAPSHOT);
      _serializer.WriteUShort((ushort) _allPlayers.Length);
      foreach (var playerPeerId in _allPlayers)
      {
        _serializer.WriteUShort((ushort) playerPeerId);
        _serializer.WriteString(_players[playerPeerId].Id);
        _serializer.WriteString(_players[playerPeerId].PlayerName);
      }

      var dynamicEntities = _entitiesAll.Where(e => e.StaticId == 0).ToArray();
      var dynamicEntitiesCount = (ushort) dynamicEntities.Length;
      _serializer.WriteUShort(dynamicEntitiesCount);
      foreach (var entity in dynamicEntities)
      {
        ReadOnlySpan<byte> payload = entity.Payload.AsSpan();

        _serializer.WriteUShort(entity.EntityType);
        _serializer.WriteUShort(entity.EntityId);
        _serializer.WriteUShort((ushort) entity.OwnerId);
        _serializer.WriteUShort((ushort) payload.Length);
        _serializer.WriteData(ref payload);

        entity.Snapshot(_serializer);
      }

      var staticEntities = _entitiesAll.Where(e => e.StaticId != 0).ToArray();
      var staticEntitiesCount = (ushort) staticEntities.Length;
      _serializer.WriteUShort(staticEntitiesCount);
      foreach (var entity in staticEntities)
      {
        ReadOnlySpan<byte> payload = entity.Payload.AsSpan();

        _serializer.WriteUShort(entity.EntityType);
        _serializer.WriteUShort(entity.EntityId);
        _serializer.WriteUShort(entity.StaticId);
        _serializer.WriteUShort(entity.OwnerId);
        _serializer.WriteUShort((ushort) payload.Length);
        _serializer.WriteData(ref payload);

        entity.Snapshot(_serializer);
      }

      var sendData = _serializer.ToArray();
      Send(peerId, sendData, DeliveryType.Reliable);
    }

    private void ReplicateProperties()
    {
      var entities = (ushort) _entitiesDirty.Count;
      if (entities > 0)
      {
        _serializer.Clear();
        _serializer.WriteOperation(RagonOperation.REPLICATE_ENTITY_STATE);
        _serializer.WriteUShort(entities);

        foreach (var entity in _entitiesDirty)
          entity.ReplicateProperties(_serializer);

        _entitiesDirty.Clear();
        _entitiesDirtySet.Clear();

        var sendData = _serializer.ToArray();
        Broadcast(_readyPlayers, sendData);
      }
    }

    public void Start()
    {
      _plugin.OnStart();
    }

    public void Stop()
    {
      foreach (var peerId in _allPlayers)
        _gameThread.Server.Disconnect(peerId, 0);

      _plugin.OnStop();
      _plugin.Detach();
    }

    public Player GetPlayerById(uint peerId) => _players[peerId];

    public Entity GetEntityById(int entityId) => _entities[entityId];

    public Player GetOwner() => _players[_owner];

    public IDispatcher GetThreadDispatcher() => _gameThread.ThreadDispatcher;

    public IScheduler GetScheduler() => _scheduler;

    public void Send(uint peerId, byte[] rawData, DeliveryType deliveryType = DeliveryType.Unreliable) =>
      _gameThread.Server.Send(peerId, rawData, deliveryType);

    public void Broadcast(uint[] peersIds, byte[] rawData, DeliveryType deliveryType = DeliveryType.Unreliable) =>
      _gameThread.Server.Broadcast(peersIds, rawData, deliveryType);

    public void Broadcast(byte[] rawData, DeliveryType deliveryType = DeliveryType.Unreliable) =>
      _gameThread.Server.Broadcast(_allPlayers, rawData, deliveryType);
  }
}