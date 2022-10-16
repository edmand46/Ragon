using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    private Dictionary<ushort, Player> _players = new();
    private Dictionary<int, Entity> _entities = new();
    private ushort _owner;

    private readonly IScheduler _scheduler;
    private readonly ISocketServer _socketServer;
    private readonly Application _application;
    private readonly PluginBase _plugin;
    private readonly RagonSerializer _reader = new(512);
    private readonly RagonSerializer _writer = new(512);

    // Cache
    private ushort[] _readyPlayers = Array.Empty<ushort>();
    private ushort[] _allPlayers = Array.Empty<ushort>();
    private Entity[] _entitiesAll = Array.Empty<Entity>();
    private HashSet<Entity> _entitiesDirtySet = new HashSet<Entity>();
    private List<Entity> _entitiesDirty = new List<Entity>();
    private List<ushort> _peersCache = new List<ushort>();
    private List<ushort> _awaitingPeers = new List<ushort>();

    public Player GetPlayerById(ushort peerId) => _players[peerId];

    public Entity GetEntityById(int entityId) => _entities[entityId];

    public Player GetOwner() => _players[_owner];

    public RagonSerializer GetSharedSerializer() => _writer;

    public GameRoom(Application application, PluginBase pluginBase, string roomId, string map, int min, int max)
    {
      _application = application;
      _socketServer = application.SocketServer;
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
        _reader.Clear();
        _reader.WriteOperation(RagonOperation.JOIN_SUCCESS);
        _reader.WriteString(Id);
        _reader.WriteString(player.Id);
        _reader.WriteString(GetOwner().Id);
        _reader.WriteUShort((ushort) PlayersMin);
        _reader.WriteUShort((ushort) PlayersMax);

        var sendData = _reader.ToArray();
        Send(player.PeerId, sendData, DeliveryType.Reliable);
      }

      {
        _reader.Clear();
        _reader.WriteOperation(RagonOperation.LOAD_SCENE);
        _reader.WriteString(Map);

        var sendData = _reader.ToArray();
        Send(player.PeerId, sendData, DeliveryType.Reliable);
      }
    }

    public void RemovePlayer(ushort peerId)
    {
      if (_players.Remove(peerId, out var player))
      {
        _allPlayers = _players.Select(p => p.Key).ToArray();
        _readyPlayers = _players.Where(p => p.Value.IsLoaded).Select(p => p.Key).ToArray();

        _plugin.OnPlayerLeaved(player);

        SendLeaved(player);

        if (_allPlayers.Length > 0 && player.PeerId == _owner)
        {
          var nextOwnerId = _allPlayers[0];
          var nextOwner = _players[nextOwnerId];

          _owner = nextOwnerId;

          SendChangeOwner(player, nextOwner);
        }

        _entitiesAll = _entities.Values.ToArray();
      }
    }

    public void ProcessEvent(ushort peerId, RagonOperation operation, ReadOnlySpan<byte> payloadRawData)
    {
      _reader.Clear();
      _reader.FromSpan(ref payloadRawData);

      switch (operation)
      {
        case RagonOperation.LOAD_SCENE:
        {
          var sceneName = _reader.ReadString();
          SendScene(sceneName);
          break;
        }
        case RagonOperation.SCENE_LOADED:
        {
          var player = _players[peerId];
          if (peerId == _owner)
          {
            var statics = _reader.ReadUShort();
            for (var staticIndex = 0; staticIndex < statics; staticIndex++)
            {
              var entityType = _reader.ReadUShort();
              var entityAuthority = (RagonAuthority) _reader.ReadByte();
              var staticId = _reader.ReadUShort();
              var propertiesCount = _reader.ReadUShort();

              var entity = new Entity(this, player.PeerId, entityType, staticId, entityAuthority);
              for (var propertyIndex = 0; propertyIndex < propertiesCount; propertyIndex++)
              {
                var propertyType = _reader.ReadBool();
                var propertySize = _reader.ReadUShort();
                entity.AddProperty(new EntityProperty(propertySize, propertyType));
              }
              
              player.AttachEntity(entity);
              
              AttachEntity(entity);
            }

            _entitiesAll = _entities.Values.ToArray();
            _logger.Trace($"Scene entities: {statics}");

            _awaitingPeers.Add(peerId);

            foreach (var peer in _awaitingPeers)
            {
              var joinedPlayer = _players[peer];
              joinedPlayer.IsLoaded = true;
              _plugin.OnPlayerJoined(joinedPlayer);
              _logger.Trace($"[{_owner}][{peer}] Player {joinedPlayer.Id} restored");

              SendJoined(player, peerId);
            }

            _readyPlayers = _players.Where(p => p.Value.IsLoaded).Select(p => p.Key).ToArray();
            foreach (var peer in _awaitingPeers)
            {
              SendSnapshot(peer);
            }

            _awaitingPeers.Clear();
          }
          else if (GetOwner().IsLoaded)
          {
            _logger.Trace($"[{_owner}][{peerId}] Player {player.Id} restored instantly");
            player.IsLoaded = true;

            SendJoined(player, peerId);

            _readyPlayers = _players.Where(p => p.Value.IsLoaded).Select(p => p.Key).ToArray();
            _plugin.OnPlayerJoined(player);

            SendSnapshot(peerId);

            foreach (var (key, value) in _entities)
              value.RestoreBufferedEvents(peerId);
          }
          else
          {
            _logger.Trace($"[{_owner}][{peerId}] Player {player.Id} waiting");
            _awaitingPeers.Add(peerId);
          }

          break;
        }
        case RagonOperation.REPLICATE_ENTITY_STATE:
        {
          var entitiesCount = _reader.ReadUShort();
          for (var entityIndex = 0; entityIndex < entitiesCount; entityIndex++)
          {
            var entityId = _reader.ReadUShort();
            if (_entities.TryGetValue(entityId, out var entity))
            {
              entity.HandleState(peerId, _reader);

              if (_entitiesDirtySet.Add(entity))
                _entitiesDirty.Add(entity);
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
          var eventId = _reader.ReadUShort();
          var eventMode = (RagonReplicationMode) _reader.ReadByte();
          var targetMode = (RagonTarget) _reader.ReadByte();
          var entityId = _reader.ReadUShort();
          var payloadData = _reader.ReadData(_reader.Size);

          Span<byte> payloadRaw = stackalloc byte[_reader.Size];
          ReadOnlySpan<byte> payload = payloadRaw;
          payloadData.CopyTo(payloadRaw);

          if (!_entities.TryGetValue(entityId, out var ent))
          {
            _logger.Warn($"Entity not found for event with Id {eventId}");
            return;
          }

          if (_plugin.InternalHandle(peerId, entityId, eventId, ref payload))
            return;

          ent.ReplicateEvent(peerId, eventId, payload, eventMode, targetMode);
          break;
        }
        case RagonOperation.CREATE_ENTITY:
        {
          var entityType = _reader.ReadUShort();
          var eventAuthority = (RagonAuthority) _reader.ReadByte();
          var propertiesCount = _reader.ReadUShort();

          _logger.Trace($"[{peerId}] Create Entity {entityType}");

          var player = _players[peerId];
          var entity = new Entity(this, (ushort) player.PeerId, entityType, 0, eventAuthority);
          for (var i = 0; i < propertiesCount; i++)
          {
            var propertyType = _reader.ReadBool();
            var propertySize = _reader.ReadUShort();
            entity.AddProperty(new EntityProperty(propertySize, propertyType));
          }

          var entityPayload = _reader.ReadData(_reader.Size);
          entity.SetPayload(entityPayload.ToArray());

          if (_plugin.OnEntityCreated(player, entity))
            return;

          player.AttachEntity(entity);
          AttachEntity(entity);

          entity.SendCreate();
          break;
        }
        case RagonOperation.DESTROY_ENTITY:
        {
          var entityId = _reader.ReadInt();
          if (_entities.TryGetValue(entityId, out var entity))
          {
            if (entity.Authority == RagonAuthority.OwnerOnly && entity.OwnerId != peerId)
              return;

            var player = _players[peerId];
            var destroyPayload = _reader.ReadData(_reader.Size);

            player.DetachEntity(entity);
            DetachEntity(entity);

            if (_plugin.OnEntityDestroyed(player, entity))
            {
              return;
            }

            entity.SendDestroy(destroyPayload);
          }

          break;
        }
      }
    }

    public void Tick(float deltaTime)
    {
      _scheduler.Tick(deltaTime);

      SendChanges();
    }

    public void OnStart()
    {
      _plugin.OnStart();
    }

    public void OnStop()
    {
      foreach (var peerId in _allPlayers)
        _application.SocketServer.Disconnect(peerId, 0);

      _plugin.OnStop();
      _plugin.Detach();
    }

    public void AttachEntity(Entity entity)
    {
      _entities.Add(entity.EntityId, entity);
      _entitiesAll = _entities.Values.ToArray();
    }

    public void DetachEntity(Entity entity)
    {
      _entities.Remove(entity.EntityId);
      _entitiesAll = _entities.Values.ToArray();
    }

    void SendChangeOwner(Player prev, Player next)
    {
      var entitiesToUpdate = prev.Entities.Where(e => e.StaticId > 0).ToArray();

      _reader.Clear();
      _reader.WriteOperation(RagonOperation.OWNERSHIP_CHANGED);
      _reader.WriteString(next.Id);
      _reader.WriteUShort((ushort) entitiesToUpdate.Length);
      foreach (var entity in entitiesToUpdate)
      {
        _reader.WriteUShort(entity.EntityId);
        entity.SetOwner((ushort) next.PeerId);
      }

      var sendData = _reader.ToArray();
      Broadcast(_readyPlayers, sendData);
    }

    void SendJoined(Player player, uint excludePeerId)
    {
      _reader.Clear();
      _reader.WriteOperation(RagonOperation.PLAYER_JOINED);
      _reader.WriteUShort((ushort) player.PeerId);
      _reader.WriteString(player.Id);
      _reader.WriteString(player.PlayerName);

      var sendData = _reader.ToArray();
      var readyPlayersWithExcludedPeer = _readyPlayers.Where(p => p != excludePeerId).ToArray();
      Broadcast(readyPlayersWithExcludedPeer, sendData, DeliveryType.Reliable);
    }

    void SendLeaved(Player player)
    {
      _reader.Clear();
      _reader.WriteOperation(RagonOperation.PLAYER_LEAVED);
      _reader.WriteString(player.Id);

      var entitiesToDelete = player.Entities.Where(e => e.StaticId == 0).ToArray();
      _reader.WriteUShort((ushort) entitiesToDelete.Length);
      foreach (var entity in entitiesToDelete)
      {
        _reader.WriteUShort(entity.EntityId);
        _entities.Remove(entity.EntityId);
      }

      var sendData = _reader.ToArray();
      Broadcast(_readyPlayers, sendData);
    }

    void SendSnapshot(ushort peerId)
    {
      _reader.Clear();
      _reader.WriteOperation(RagonOperation.SNAPSHOT);
      _reader.WriteUShort((ushort) _readyPlayers.Length);
      foreach (var playerPeerId in _readyPlayers)
      {
        _reader.WriteUShort(playerPeerId);
        _reader.WriteString(_players[playerPeerId].Id);
        _reader.WriteString(_players[playerPeerId].PlayerName);
      }

      var dynamicEntities = _entitiesAll.Where(e => e.StaticId == 0).ToArray();
      var dynamicEntitiesCount = (ushort) dynamicEntities.Length;
      _reader.WriteUShort(dynamicEntitiesCount);
      foreach (var entity in dynamicEntities)
      {
        ReadOnlySpan<byte> payload = entity.Payload.AsSpan();

        _reader.WriteUShort(entity.EntityType);
        _reader.WriteUShort(entity.EntityId);
        _reader.WriteUShort(entity.OwnerId);
        _reader.WriteUShort((ushort) payload.Length);
        _reader.WriteData(ref payload);

        entity.WriteSnapshot(_reader);
      }

      var staticEntities = _entitiesAll.Where(e => e.StaticId != 0).ToArray();
      var staticEntitiesCount = (ushort) staticEntities.Length;
      _reader.WriteUShort(staticEntitiesCount);
      foreach (var entity in staticEntities)
      {
        ReadOnlySpan<byte> payload = entity.Payload.AsSpan();

        _reader.WriteUShort(entity.EntityType);
        _reader.WriteUShort(entity.EntityId);
        _reader.WriteUShort(entity.StaticId);
        _reader.WriteUShort(entity.OwnerId);
        _reader.WriteUShort((ushort) payload.Length);
        _reader.WriteData(ref payload);

        entity.WriteSnapshot(_reader);
      }

      var sendData = _reader.ToArray();
      Send(peerId, sendData, DeliveryType.Reliable);
    }

    void SendChanges()
    {
      var entities = (ushort) _entitiesDirty.Count;
      if (entities > 0)
      {
        _reader.Clear();
        _reader.WriteOperation(RagonOperation.REPLICATE_ENTITY_STATE);
        _reader.WriteUShort(entities);

        foreach (var entity in _entitiesDirty)
          entity.WriteProperties(_reader);

        _entitiesDirty.Clear();
        _entitiesDirtySet.Clear();

        var sendData = _reader.ToArray();
        Broadcast(_readyPlayers, sendData);
      }
    }

    void SendScene(string sceneName)
    {
      _readyPlayers = Array.Empty<ushort>();
      _entitiesAll = Array.Empty<Entity>();
      _entities.Clear();

      _reader.Clear();
      _reader.WriteOperation(RagonOperation.LOAD_SCENE);
      _reader.WriteString(sceneName);

      var sendData = _reader.ToArray();
      Broadcast(_allPlayers, sendData, DeliveryType.Reliable);
    }

    public void Send(ushort peerId, byte[] rawData, DeliveryType deliveryType = DeliveryType.Unreliable) =>
      _socketServer.Send(peerId, rawData, deliveryType);

    public void Broadcast(ushort[] peersIds, byte[] rawData, DeliveryType deliveryType = DeliveryType.Unreliable) =>
      _socketServer.Broadcast(peersIds, rawData, deliveryType);

    public void BroadcastToAll(byte[] rawData, DeliveryType deliveryType = DeliveryType.Unreliable) =>
      _socketServer.Broadcast(_allPlayers, rawData, deliveryType);

    public void BroadcastToReady(byte[] rawData, DeliveryType deliveryType = DeliveryType.Unreliable) =>
      _socketServer.Broadcast(_readyPlayers, rawData, deliveryType);

    public void BroadcastToReady(byte[] rawData, ushort[] excludePeersIds, DeliveryType deliveryType = DeliveryType.Unreliable)
    {
      _peersCache.Clear();
      foreach (var playerPeerId in _readyPlayers)
      {
        foreach (var excludePeersId in excludePeersIds)
        {
          if (playerPeerId != excludePeersId)
          {
            _peersCache.Add(playerPeerId);
          }
        }
      }
      Broadcast(_peersCache.ToArray(), rawData, deliveryType);
    }
  }
}