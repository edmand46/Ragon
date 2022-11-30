using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using Ragon.Common;

namespace Ragon.Core
{
  public class GameRoom 
  {
    public int PlayersMin { get; private set; }
    public int PlayersMax { get; private set; }
    public int PlayersCount => _players.Count;
    public int EntitiesCount => _entities.Count;
    public string Id { get; private set; }
    public string Map { get; private set; }
    public PluginBase Plugin => _plugin;
    private ILogger _logger = LogManager.GetCurrentClassLogger();
    private Dictionary<ushort, Player> _players = new();
    private Dictionary<int, Entity> _entities = new();
    private ushort _owner;

    private readonly ISocketServer _socketServer;
    private readonly Scheduler _scheduler;
    private readonly Application _application;
    private readonly PluginBase _plugin;
    private readonly RagonSerializer _writer = new(512);

    // Cache
    private ushort[] _readyPlayers = Array.Empty<ushort>();
    private ushort[] _allPlayers = Array.Empty<ushort>();
    private Entity[] _entitiesAll = Array.Empty<Entity>();
    private HashSet<Entity> _entitiesDirtySet = new HashSet<Entity>();
    private List<Entity> _entitiesDirty = new List<Entity>();
    private List<ushort> _peersCache = new List<ushort>();
    private List<ushort> _awaitingPeers = new List<ushort>();

    public Player GetPlayerByPeer(ushort peerId) => _players[peerId];
    
    public Player GetPlayerById(string id) => _players.Values.FirstOrDefault(p => p.Id == id)!;

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

      AcceptPlayer(player);
    }

    public void RemovePlayer(ushort peerId)
    {
      if (_players.Remove(peerId, out var player))
      {
        _allPlayers = _players.Select(p => p.Key).ToArray();
        _readyPlayers = _players.Where(p => p.Value.IsLoaded).Select(p => p.Key).ToArray();

        _plugin.OnPlayerLeaved(player);

        BroadcastLeaved(player);

        if (_allPlayers.Length > 0 && player.PeerId == _owner)
        {
          var nextOwnerId = _allPlayers[0];
          var nextOwner = _players[nextOwnerId];

          _owner = nextOwnerId;

          BroadcastNewOwner(player, nextOwner);
        }

        _entitiesAll = _entities.Values.ToArray();
      }
    }

    public void ProcessEvent(ushort peerId, RagonOperation operation, RagonSerializer reader)
    {
      switch (operation)
      {
        case RagonOperation.LOAD_SCENE:
        {
          var sceneName = reader.ReadString();
          BroadcastNewScene(sceneName);
          break;
        }
        case RagonOperation.SCENE_LOADED:
        {
          var player = _players[peerId];
          if (peerId == _owner)
          {
            var statics = reader.ReadUShort();
            for (var staticIndex = 0; staticIndex < statics; staticIndex++)
            {
              var entityType = reader.ReadUShort();
              var entityAuthority = (RagonAuthority) reader.ReadByte();
              var staticId = reader.ReadUShort();
              var propertiesCount = reader.ReadUShort();

              var entity = new Entity(this, player.PeerId, entityType, staticId, entityAuthority);
              for (var propertyIndex = 0; propertyIndex < propertiesCount; propertyIndex++)
              {
                var propertyType = reader.ReadBool();
                var propertySize = reader.ReadUShort();
                entity.AddProperty(new EntityProperty(propertySize, propertyType));
              }

              player.AttachEntity(entity);
              AttachEntity(player, entity);
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

              BroadcastJoined(player);
            }

            _readyPlayers = _players.Where(p => p.Value.IsLoaded).Select(p => p.Key).ToArray();
            
            BroadcastSnapshot(_awaitingPeers.ToArray());
            
            _awaitingPeers.Clear();
          }
          else if (GetOwner().IsLoaded)
          {
            _logger.Trace($"[{_owner}][{peerId}] Player {player.Id} restored instantly");
            player.IsLoaded = true;

            BroadcastJoined(player);

            _readyPlayers = _players.Where(p => p.Value.IsLoaded).Select(p => p.Key).ToArray();
            _plugin.OnPlayerJoined(player);

            BroadcastSnapshot(new[] {peerId});

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
          var entitiesCount = reader.ReadUShort();
          for (var entityIndex = 0; entityIndex < entitiesCount; entityIndex++)
          {
            var entityId = reader.ReadUShort();
            if (_entities.TryGetValue(entityId, out var entity))
            {
              entity.ReadState(peerId, reader);

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
          var entityId = reader.ReadUShort();
          if (!_entities.TryGetValue(entityId, out var ent))
          {
            _logger.Warn($"Entity not found for event with Id {entityId}");
            return;
          }
          
          var player = _players[peerId];
          ent.ProcessEvent(player, reader);
          break;
        }
        case RagonOperation.CREATE_ENTITY:
        {
          var entityType = reader.ReadUShort();
          var eventAuthority = (RagonAuthority) reader.ReadByte();
          var propertiesCount = reader.ReadUShort();

          _logger.Trace($"[{peerId}] Create Entity {entityType}");

          var player = _players[peerId];
          var entity = new Entity(this, player.PeerId, entityType, 0, eventAuthority);
          for (var i = 0; i < propertiesCount; i++)
          {
            var propertyType = reader.ReadBool();
            var propertySize = reader.ReadUShort();
            entity.AddProperty(new EntityProperty(propertySize, propertyType));
          }

          var entityPayload = reader.ReadData(reader.Size);
          entity.SetPayload(entityPayload.ToArray());

          if (_plugin.OnEntityCreated(player, entity))
            return;

          player.AttachEntity(entity);
          AttachEntity(player, entity);

          entity.Create();
          break;
        }
        case RagonOperation.DESTROY_ENTITY:
        {
          var entityId = reader.ReadInt();
          if (_entities.TryGetValue(entityId, out var entity))
          { 
            var player = _players[peerId];
            var payload = reader.ReadData(reader.Size);
            DetachEntity(player, entity, payload);
          }
          break;
        }
      }
    }

    public void Tick(float deltaTime)
    {
      _scheduler.Tick(deltaTime);
      BroadcastState();
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
      _scheduler.Dispose();
    }

    public void AttachEntity(Player player, Entity entity)
    {
      _entities.Add(entity.EntityId, entity);
      _entitiesAll = _entities.Values.ToArray();
    }

    public void DetachEntity(Player player, Entity entity, ReadOnlySpan<byte> payload)
    {
      if (entity.Authority == RagonAuthority.OwnerOnly && entity.OwnerId != player.PeerId)
        return;
      
      if (_plugin.OnEntityDestroyed(player, entity))
        return;
      
      player.DetachEntity(entity);
      entity.Destroy(payload);
      
      _entities.Remove(entity.EntityId);
      _entitiesAll = _entities.Values.ToArray();
    }

    void BroadcastNewOwner(Player prev, Player next)
    {
      var entitiesToUpdate = prev.Entities.Where(e => e.StaticId > 0).ToArray();

      _writer.Clear();
      _writer.WriteOperation(RagonOperation.OWNERSHIP_CHANGED);
      _writer.WriteString(next.Id);
      _writer.WriteUShort((ushort) entitiesToUpdate.Length);
      
      foreach (var entity in entitiesToUpdate)
      {
        _writer.WriteUShort(entity.EntityId);
        entity.SetOwner((ushort) next.PeerId);
      }

      BroadcastToReady(_writer, DeliveryType.Reliable);
    }

    void BroadcastJoined(Player player)
    {
      _writer.Clear();
      _writer.WriteOperation(RagonOperation.PLAYER_JOINED);
      _writer.WriteUShort(player.PeerId);
      _writer.WriteString(player.Id);
      _writer.WriteString(player.PlayerName);
      
      BroadcastToReady(_writer, new [] { player.PeerId }, DeliveryType.Reliable);
    }

    void BroadcastLeaved(Player player)
    {
      _writer.Clear();
      _writer.WriteOperation(RagonOperation.PLAYER_LEAVED);
      _writer.WriteString(player.Id);

      var entitiesToDelete = player.Entities.Where(e => e.StaticId == 0).ToArray();
      _writer.WriteUShort((ushort) entitiesToDelete.Length);
      foreach (var entity in entitiesToDelete)
      {
        _writer.WriteUShort(entity.EntityId);
        _entities.Remove(entity.EntityId);
      }

      BroadcastToReady(_writer, DeliveryType.Reliable);
    }

    void BroadcastSnapshot(ushort[] peersIds)
    {
      _writer.Clear();
      _writer.WriteOperation(RagonOperation.SNAPSHOT);
      _writer.WriteUShort((ushort) _readyPlayers.Length);
      foreach (var playerPeerId in _readyPlayers)
      {
        _writer.WriteUShort(playerPeerId);
        _writer.WriteString(_players[playerPeerId].Id);
        _writer.WriteString(_players[playerPeerId].PlayerName);
      }

      var dynamicEntities = _entitiesAll.Where(e => e.StaticId == 0).ToArray();
      var dynamicEntitiesCount = (ushort) dynamicEntities.Length;
      _writer.WriteUShort(dynamicEntitiesCount);
      foreach (var entity in dynamicEntities)
      {
        ReadOnlySpan<byte> payload = entity.Payload.AsSpan();

        _writer.WriteUShort(entity.EntityType);
        _writer.WriteUShort(entity.EntityId);
        _writer.WriteUShort(entity.OwnerId);
        _writer.WriteUShort((ushort) payload.Length);
        _writer.WriteData(ref payload);

        entity.WriteSnapshot(_writer);
      }

      var staticEntities = _entitiesAll.Where(e => e.StaticId != 0).ToArray();
      var staticEntitiesCount = (ushort) staticEntities.Length;
      _writer.WriteUShort(staticEntitiesCount);
      foreach (var entity in staticEntities)
      {
        ReadOnlySpan<byte> payload = entity.Payload.AsSpan();

        _writer.WriteUShort(entity.EntityType);
        _writer.WriteUShort(entity.EntityId);
        _writer.WriteUShort(entity.StaticId);
        _writer.WriteUShort(entity.OwnerId);
        _writer.WriteUShort((ushort) payload.Length);
        _writer.WriteData(ref payload);

        entity.WriteSnapshot(_writer);
      }

      var sendData = _writer.ToArray();
      Broadcast(peersIds, sendData, DeliveryType.Reliable);
    }

    void BroadcastState()
    {
      var entities = (ushort) _entitiesDirty.Count;
      if (entities > 0)
      {
        _writer.Clear();
        _writer.WriteOperation(RagonOperation.REPLICATE_ENTITY_STATE);
        _writer.WriteUShort(entities);

        foreach (var entity in _entitiesDirty)
          entity.WriteProperties(_writer);

        _entitiesDirty.Clear();
        _entitiesDirtySet.Clear();

        BroadcastToReady(_writer, DeliveryType.Reliable);
      }
    }

    void AcceptPlayer(Player player)
    {
      _writer.Clear();
      _writer.WriteOperation(RagonOperation.JOIN_SUCCESS);
      _writer.WriteString(Id);
      _writer.WriteString(player.Id);
      _writer.WriteString(GetOwner().Id);
      _writer.WriteUShort((ushort) PlayersMin);
      _writer.WriteUShort((ushort) PlayersMax);
      _writer.WriteString(Map);

      Send(player.PeerId, _writer, DeliveryType.Reliable);
    }

    void BroadcastNewScene(string sceneName)
    {
      _readyPlayers = Array.Empty<ushort>();
      _entitiesAll = Array.Empty<Entity>();
      _entities.Clear();

      _writer.Clear();
      _writer.WriteOperation(RagonOperation.LOAD_SCENE);
      _writer.WriteString(sceneName);

      BroadcastToAll(_writer, DeliveryType.Reliable);
    }

    public void Send(ushort peerId, byte[] rawData, DeliveryType deliveryType = DeliveryType.Unreliable)
    {
      _socketServer.Send(peerId, rawData, deliveryType);
    }

    public void Send(ushort peerId, RagonSerializer writer, DeliveryType deliveryType = DeliveryType.Unreliable)
    {
      var sendData = writer.ToArray();
      _socketServer.Send(peerId, sendData, deliveryType);
    }

    public void Broadcast(ushort[] peersIds, byte[] rawData, DeliveryType deliveryType = DeliveryType.Unreliable)
    {
      _socketServer.Broadcast(peersIds, rawData, deliveryType);
    }

    public void BroadcastToAll(byte[] rawData, DeliveryType deliveryType = DeliveryType.Unreliable)
    {
      _socketServer.Broadcast(_allPlayers, rawData, deliveryType);
    }

    public void BroadcastToAll(RagonSerializer writer, DeliveryType deliveryType = DeliveryType.Unreliable)
    {
      var sendData = writer.ToArray();
      _socketServer.Broadcast(_allPlayers, sendData, deliveryType);
    }

    public void BroadcastToReady(byte[] rawData, DeliveryType deliveryType = DeliveryType.Unreliable)
    {
      _socketServer.Broadcast(_readyPlayers, rawData, deliveryType);
    }

    public void BroadcastToReady(RagonSerializer writer, DeliveryType deliveryType = DeliveryType.Unreliable)
    {
      var sendData = writer.ToArray();
      _socketServer.Broadcast(_readyPlayers, sendData, deliveryType);
    }

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

      var peersIds = _peersCache.ToArray();
      _socketServer.Broadcast(peersIds, rawData, deliveryType);
    }

    public void BroadcastToReady(RagonSerializer writer, ushort[] excludePeersIds, DeliveryType deliveryType = DeliveryType.Unreliable)
    {
      var sendData = writer.ToArray();
      BroadcastToReady(sendData, excludePeersIds, deliveryType);
    }
  }
}