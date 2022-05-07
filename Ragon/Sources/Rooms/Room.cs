using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using NetStack.Serialization;
using NLog;
using Ragon.Common;

namespace Ragon.Core
{
  public class Room : IDisposable
  {
    public int PlayersMin { get; private set; }
    public int PlayersMax { get; private set; }
    public int PlayersCount => _players.Count;
    public string Id { get; private set; }
    public string Map { get; private set; }

    private ILogger _logger = LogManager.GetCurrentClassLogger();
    private Dictionary<uint, Player> _players = new();
    private Dictionary<int, Entity> _entities = new();
    private uint _owner;

    private readonly PluginBase _plugin;
    private readonly RoomThread _roomThread;

    // Cache
    private uint[] _readyPlayers = Array.Empty<uint>();
    private uint[] _allPlayers = Array.Empty<uint>();
    private Entity[] _entitiesAll = Array.Empty<Entity>();

    public Room(RoomThread roomThread, PluginBase pluginBase, string map, int min, int max)
    {
      _roomThread = roomThread;
      _plugin = pluginBase;

      Map = map;
      PlayersMin = min;
      PlayersMax = max;
      Id = Guid.NewGuid().ToString();

      _logger.Info($"Room created with plugin: {_plugin.GetType().Name}");
      _plugin.Attach(this);
    }

    public void Joined(uint peerId, ReadOnlySpan<byte> payload)
    {
      if (_players.Count == 0)
      {
        _owner = peerId;
      }

      var player = new Player()
      {
        PlayerName = "Player " + peerId,
        PeerId = peerId,
        IsLoaded = false,
        Entities = new List<Entity>(),
        EntitiesIds = new List<int>(),
      };

      _players.Add(peerId, player);
      _allPlayers = _players.Select(p => p.Key).ToArray();

      {
        var idRaw = Encoding.UTF8.GetBytes(Id).AsSpan();

        var sendData = new byte[idRaw.Length + 18];
        var data = sendData.AsSpan();
        
        Span<byte> operationData = data.Slice(0, 2);
        Span<byte> peerData = data.Slice(2, 4);
        Span<byte> ownerData = data.Slice(4, 4);
        Span<byte> minData = data.Slice(10, 4);
        Span<byte> maxData = data.Slice(14, 4);
        Span<byte> idData = data.Slice(18, idRaw.Length);

        RagonHeader.WriteUShort((ushort) RagonOperation.JOIN_ROOM, ref operationData);
        RagonHeader.WriteInt((int) peerId, ref peerData);
        RagonHeader.WriteInt((int) _owner, ref ownerData);
        RagonHeader.WriteInt(PlayersMin, ref minData);
        RagonHeader.WriteInt(PlayersMax, ref maxData);

        idRaw.CopyTo(idData);
        
        Send(peerId, sendData);
      }

      {
        var sceneRawData = Encoding.UTF8.GetBytes(Map).AsSpan();
        var sendData = new byte[sceneRawData.Length + 2];
        var data = sendData.AsSpan();
        
        Span<byte> operationData = data.Slice(0, 2);
        Span<byte> sceneData = data.Slice(2, sceneRawData.Length);

        RagonHeader.WriteUShort((ushort) RagonOperation.LOAD_SCENE, ref operationData);
        sceneRawData.CopyTo(sceneData);

        Send(peerId, sendData, DeliveryType.Reliable);
      }
    }

    public void Leave(uint peerId)
    {
      if (_players.Remove(peerId, out var player))
      {
        _allPlayers = _players.Select(p => p.Key).ToArray();

        _plugin.OnPlayerLeaved(player);

        foreach (var entityId in player.EntitiesIds)
        {
          var sendData = new byte[6];
          var entityData = sendData.AsSpan();
          var operationData = entityData.Slice(0, 2);

          RagonHeader.WriteUShort((ushort) RagonOperation.DESTROY_ENTITY, ref operationData);
          RagonHeader.WriteInt(entityId, ref entityData);

          Broadcast(_allPlayers, sendData);

          _entities.Remove(entityId);
        }
      }
    }

    public void ProcessEvent(RagonOperation operation, uint peerId, ReadOnlySpan<byte> rawData)
    {
      switch (operation)
      {
        case RagonOperation.REPLICATE_ENTITY_STATE:
        {
          var entityData = rawData.Slice(2, 4);
          var entityId = RagonHeader.ReadInt(ref entityData);
          if (_entities.TryGetValue(entityId, out var ent) && ent.OwnerId == peerId)
          {
            ent.State = rawData.Slice(6, rawData.Length - 6).ToArray();

            var data = new byte[rawData.Length];
            
            rawData.CopyTo(data);
            
            Broadcast(_readyPlayers, data);
          }

          break;
        }
        case RagonOperation.REPLICATE_ENTITY_PROPERTY:
        {
          var entityData = rawData.Slice(2, 4);
          var entityId = RagonHeader.ReadInt(ref entityData);
          if (_entities.TryGetValue(entityId, out var ent) && ent.OwnerId == peerId)
          {
            var propertyData = rawData.Slice(6, 4);
            var propertyId = RagonHeader.ReadInt(ref propertyData);
            var payload = rawData.Slice(10, rawData.Length - 10).ToArray();
            var props = _entities[entityId].Properties;

            if (props.ContainsKey(propertyId))
              props[propertyId] = payload;
            else
              props.Add(propertyId, payload);

            var sendData = new byte[rawData.Length];
            
            rawData.CopyTo(sendData);

            Broadcast(_readyPlayers, sendData, DeliveryType.Reliable);
          }

          break;
        }
        case RagonOperation.REPLICATE_ENTITY_EVENT:
        {
          var evntCodeData = rawData.Slice(2, 2);
          var entityIdData = rawData.Slice(4, 4);
          var evntId = RagonHeader.ReadUShort(ref evntCodeData);
          var entityId = RagonHeader.ReadInt(ref entityIdData);

          if (_entities[entityId].OwnerId != peerId)
            return;

          var payload = rawData.Slice(8, rawData.Length - 8);
          if (_plugin.InternalHandle(peerId, entityId, evntId, ref payload))
            return;

          var data = new byte[rawData.Length];

          rawData.CopyTo(data);

          Broadcast(_readyPlayers, data, DeliveryType.Reliable);
          break;
        }
        case RagonOperation.REPLICATE_EVENT:
        {
          var evntCodeData = rawData.Slice(2, 2);
          var evntId = RagonHeader.ReadUShort(ref evntCodeData);

          var payload = rawData.Slice(4, rawData.Length - 4);
          if (_plugin.InternalHandle(peerId, evntId, ref payload))
            return;

          var data = new byte[rawData.Length];
          
          rawData.CopyTo(data);
          
          Broadcast(_readyPlayers, data, DeliveryType.Reliable);
          break;
        }
        case RagonOperation.CREATE_ENTITY:
        {
          var typeData = rawData.Slice(2, 2);
          var entityPayloadData = rawData.Slice(4, rawData.Length - 4);

          var entityType = RagonHeader.ReadUShort(ref typeData);
          var entity = new Entity(peerId, entityType);
          entity.State = entityPayloadData.ToArray();
          entity.Properties = new Dictionary<int, byte[]>();

          var player = _players[peerId];
          player.Entities.Add(entity);
          player.EntitiesIds.Add(entity.EntityId);

          _entities.Add(entity.EntityId, entity);
          _entitiesAll = _entities.Values.ToArray();

          _plugin.OnEntityCreated(player, entity);

          var data = new byte[entityPayloadData.Length + 12];
          var sendData = data.AsSpan();
          var operationData = sendData.Slice(0, 2);
          var entityTypeData = sendData.Slice(2, 4);
          var entityData = sendData.Slice(4, 4);
          var peerData = sendData.Slice(8, 4);
          var payload = sendData.Slice(12, entityPayloadData.Length);

          entityPayloadData.CopyTo(payload);

          RagonHeader.WriteUShort((ushort) RagonOperation.CREATE_ENTITY, ref operationData);
          RagonHeader.WriteUShort(entityType, ref entityTypeData);
          RagonHeader.WriteInt(entity.EntityId, ref entityData);
          RagonHeader.WriteInt((int) peerId, ref peerData);

          Broadcast(_allPlayers, data, DeliveryType.Reliable);
          break;
        }
        case RagonOperation.DESTROY_ENTITY:
        {
          var entityData = rawData.Slice(2, 4);
          var entityId = RagonHeader.ReadInt(ref entityData);
          if (_entities.TryGetValue(entityId, out var entity))
          {
            if (entity.OwnerId == peerId)
            {
              var player = _players[peerId];

              player.Entities.Remove(entity);
              player.EntitiesIds.Remove(entity.EntityId);

              _entities.Remove(entityId);
              _entitiesAll = _entities.Values.ToArray();

              _plugin.OnEntityDestroyed(player, entity);

              var data = new byte[rawData.Length];
              Span<byte> sendData = data.AsSpan();
              rawData.CopyTo(sendData);
              
              Broadcast(_readyPlayers, data, DeliveryType.Reliable);
            }
          }

          break;
        }
        case RagonOperation.SCENE_IS_LOADED:
        {
          Send(peerId, RagonOperation.RESTORE_BEGIN);
          foreach (var entity in _entities.Values)
          {
            var entityState = entity.State.AsSpan();
            var data = new byte[entity.State.Length + 10];
            
            Span<byte> sendData = data.AsSpan();
            Span<byte> operationData = sendData.Slice(0, 2);
            Span<byte> entityData = sendData.Slice(2, 4);
            Span<byte> ownerData = sendData.Slice(6, 4);
            Span<byte> entityStateData = sendData.Slice(10, entity.State.Length);

            RagonHeader.WriteUShort((ushort) RagonOperation.CREATE_ENTITY, ref operationData);
            RagonHeader.WriteInt(entity.EntityId, ref entityData);
            RagonHeader.WriteInt((int) entity.OwnerId, ref ownerData);
            
            entityState.CopyTo(entityStateData);
            
            Send(peerId, data, DeliveryType.Reliable);
          }

          Send(peerId, RagonOperation.RESTORE_END);
          break;
        }
        case RagonOperation.RESTORED:
        {
          _players[peerId].IsLoaded = true;
          _readyPlayers = _players.Where(p => p.Value.IsLoaded).Select(p => p.Key).ToArray();

          _plugin.OnPlayerJoined(_players[peerId]);
          break;
        }
      }
    }

    public void Tick(float deltaTime)
    {
      _plugin.OnTick(deltaTime);
    }

    public void Start()
    {
      _logger.Info("Room started");
      _plugin.OnStart();
    }

    public void Stop()
    {
      _logger.Info("Room stopped");
      _plugin.OnStop();
    }


    public void Dispose()
    {
      _logger.Info("Room destroyed");
      _plugin.Dispose();
    }

    public Player GetPlayerById(uint peerId) => _players[peerId];
    public Entity GetEntityById(int entityId) => _entities[entityId];
    public Player GetOwner() => _players[_owner];

    public void Send(uint peerId, RagonOperation operation, DeliveryType deliveryType = DeliveryType.Unreliable)
    {
      var rawData = new byte[2];
      var rawDataSpan = new Span<byte>(rawData);
      
      RagonHeader.WriteUShort((ushort) operation, ref rawDataSpan);

      _roomThread.WriteOutEvent(new Event()
      {
        PeerId = peerId,
        Data = rawData,
        Type = EventType.DATA,
        Delivery = deliveryType,
      });
    }

    public void Send(uint peerId, byte[] rawData, DeliveryType deliveryType = DeliveryType.Unreliable)
    {
      _roomThread.WriteOutEvent(new Event()
      {
        PeerId = peerId,
        Data = rawData,
        Type = EventType.DATA,
        Delivery = deliveryType,
      });
    }

    public void Broadcast(uint[] peersIds, byte[] rawData, DeliveryType deliveryType = DeliveryType.Unreliable)
    {
      foreach (var peer in peersIds)
      {
        _roomThread.WriteOutEvent(new Event()
        {
          PeerId = peer,
          Data = rawData,
          Type = EventType.DATA,
          Delivery = deliveryType,
        });
      }
    }

    public void Broadcast(byte[] rawData, DeliveryType deliveryType = DeliveryType.Unreliable)
    {
      foreach (var player in _players.Values.ToArray())
      {
        _roomThread.WriteOutEvent(new Event()
        {
          PeerId = player.PeerId,
          Data = rawData,
          Type = EventType.DATA,
          Delivery = deliveryType,
        });
      }
    }
  }
}