using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using NetStack.Serialization;
using NLog;
using NLog.LayoutRenderers;
using Ragon.Common.Protocol;

namespace Ragon.Core
{
  public class Room : IDisposable
  {
    public int PlayersMin { get; private set; }
    public int PlayersMax { get; private set; }
    public int PlayersCount => _players.Count;

    public string Map { get; private set; } 
    
    private ILogger _logger = LogManager.GetCurrentClassLogger();
    private Dictionary<uint, Player> _players = new();
    private Dictionary<int, Entity> _entities = new();
    private uint _owner;

    private readonly PluginBase _plugin;
    private readonly RoomThread _roomThread;
    private ulong _ticks = 0;

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

      _logger.Info("Room created");
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
        Span<byte> data = stackalloc byte[18];
        Span<byte> operationData = data.Slice(0, 2);
        Span<byte> peerData = data.Slice(2, 4);
        Span<byte> ownerData = data.Slice(4, 4);
        Span<byte> minData = data.Slice(10, 4);
        Span<byte> maxData = data.Slice(14, 4);

        RagonHeader.WriteUShort((ushort) RagonOperation.JOIN_ROOM, ref operationData);
        RagonHeader.WriteInt((int) peerId, ref peerData);
        RagonHeader.WriteInt((int) _owner, ref ownerData);
        RagonHeader.WriteInt(PlayersMin, ref minData);
        RagonHeader.WriteInt(PlayersMax, ref maxData);

        Send(peerId, data);
      }

      {
        var sceneRawData = Encoding.UTF8.GetBytes(Map).AsSpan();
        Span<byte> data = stackalloc byte[sceneRawData.Length + 2];
        Span<byte> operationData = data.Slice(0, 2);
        Span<byte> sceneData = data.Slice(2, sceneRawData.Length);
        
        RagonHeader.WriteUShort((ushort) RagonOperation.LOAD_SCENE, ref operationData);
        sceneRawData.CopyTo(sceneData);

        Send(peerId, data, DeliveryType.Reliable);
      }
    }

    public void Leave(uint peerId)
    {
      if (_players.Remove(peerId, out var player))
      {
        _allPlayers = _players.Select(p => p.Key).ToArray();

        foreach (var entityId in player.EntitiesIds)
        {
          Span<byte> entityData = stackalloc byte[6];
          var operationData = entityData.Slice(0, 2);

          RagonHeader.WriteUShort((ushort) RagonOperation.DESTROY_ENTITY, ref operationData);
          RagonHeader.WriteInt(entityId, ref entityData);

          Broadcast(_allPlayers, entityData);

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
          if (_entities.TryGetValue(entityId, out var ent))
          {
            ent.State = rawData.Slice(6, rawData.Length - 6).ToArray();

            Span<byte> data = stackalloc byte[rawData.Length];
            rawData.CopyTo(data);
            Broadcast(_readyPlayers, data);
          }

          break;
        }
        case RagonOperation.REPLICATE_ENTITY_PROPERTY:
        {
          var entityData = rawData.Slice(2, 4);
          var entityId = RagonHeader.ReadInt(ref entityData);
          if (_entities.TryGetValue(entityId, out var ent))
          {
            var propertyData = rawData.Slice(6, 4);
            var propertyId = RagonHeader.ReadInt(ref propertyData);
            var payload = rawData.Slice(10, rawData.Length - 10).ToArray();
            var props = _entities[entityId].Properties;

            if (props.ContainsKey(propertyId))
            {
              props[propertyId] = payload;
            }
            else
            {
              props.Add(propertyId, payload);
            }

            // Span<byte> sendData = MemoryMarshal.CreateSpan(ref MemoryMarshal.GetReference(rawData), rawData.Length);

            Span<byte> sendData = stackalloc byte[rawData.Length];
            rawData.CopyTo(sendData);
            Broadcast(_readyPlayers, sendData, DeliveryType.Reliable);
          }

          break;
        }
        case RagonOperation.REPLICATE_EVENT:
        case RagonOperation.REPLICATE_ENTITY_EVENT:
        {
          Span<byte> data = stackalloc byte[rawData.Length];
          
          var evntCodeData = rawData.Slice(2, 2);
          var evntId = RagonHeader.ReadUShort(ref evntCodeData);
          if (_plugin.InternalHandle(peerId, evntId, ref rawData)) return;
          
          rawData.CopyTo(data);
          Broadcast(_readyPlayers, data, DeliveryType.Reliable);
          break;
        }
        case RagonOperation.CREATE_ENTITY:
        {
          var entity = new Entity(peerId);
          var entityPayload = rawData.Slice(2, rawData.Length - 2);
          entity.State = entityPayload.ToArray();
          entity.Properties = new Dictionary<int, byte[]>();

          var player = _players[peerId];
          player.Entities.Add(entity);
          player.EntitiesIds.Add(entity.EntityId);

          _entities.Add(entity.EntityId, entity);
          _entitiesAll = _entities.Values.ToArray();

          Span<byte> data = stackalloc byte[entityPayload.Length + 10];
          var operationData = data.Slice(0, 2);
          var entityData = data.Slice(2, 4);
          var peerData = data.Slice(6, 4);
          var payload = data.Slice(10, entityPayload.Length);

          entityPayload.CopyTo(payload);

          RagonHeader.WriteUShort((ushort) RagonOperation.CREATE_ENTITY, ref operationData);
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

              Span<byte> sendData = stackalloc byte[rawData.Length];
              rawData.CopyTo(sendData);
              Broadcast(_readyPlayers, sendData, DeliveryType.Reliable);
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

            Span<byte> sendData = stackalloc byte[entity.State.Length + 10];
            Span<byte> operationData = sendData.Slice(0, 2);
            Span<byte> entityData = sendData.Slice(2, 4);
            Span<byte> ownerData = sendData.Slice(6, 4);
            Span<byte> entityStateData = sendData.Slice(10, entity.State.Length);

            RagonHeader.WriteUShort((ushort) RagonOperation.CREATE_ENTITY, ref operationData);
            ;
            RagonHeader.WriteInt(entity.EntityId, ref entityData);
            RagonHeader.WriteInt((int) entity.OwnerId, ref ownerData);

            entityState.CopyTo(entityStateData);

            Send(peerId, sendData, DeliveryType.Reliable);
          }

          Send(peerId, RagonOperation.RESTORE_END);
          break;
        }
        case RagonOperation.RESTORED:
        {
          _players[peerId].IsLoaded = true;
          _readyPlayers = _players.Where(p => p.Value.IsLoaded).Select(p => p.Key).ToArray();
          break;
        }
      }
    }

    public void Tick(float deltaTime)
    {
      _ticks++;
      _plugin.OnTick(_ticks, deltaTime);

      // for (var i = 0; i < _entitiesAll.Length; i++)
      // {
      //   var entity = _entities[i];
      //   Span<byte> data = stackalloc byte[entity.State.Length];
      //   rawData.CopyTo(data);
      //   Broadcast(_readyPlayers, data);
      // }
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
      _plugin.Detach();
    }

    public Player GetPlayerByPeerId(uint peerId) => _players[peerId];
    public Player GetOwner() => _players[_owner];
    
    public void Send(uint peerId, RagonOperation operation, DeliveryType deliveryType = DeliveryType.Unreliable)
    {
      Span<byte> data = stackalloc byte[2];
      RagonHeader.WriteUShort((ushort) operation, ref data);

      var bytes = data.ToArray();
      _roomThread.WriteOutEvent(new Event()
      {
        PeerId = peerId,
        Data = bytes,
        Type = EventType.DATA,
        Delivery = deliveryType,
      });
    }

    public void Send(uint peerId, Span<byte> payload, DeliveryType deliveryType = DeliveryType.Unreliable)
    {
      var bytes = payload.ToArray();
      _roomThread.WriteOutEvent(new Event()
      {
        PeerId = peerId,
        Data = bytes,
        Type = EventType.DATA,
        Delivery = deliveryType,
      });
    }

    public void Broadcast(uint[] peersIds, Span<byte> rawData, DeliveryType deliveryType = DeliveryType.Unreliable)
    {
      var bytes = rawData.ToArray();
      foreach (var peer in peersIds)
      {
        _roomThread.WriteOutEvent(new Event()
        {
          PeerId = peer,
          Data = bytes,
          Type = EventType.DATA,
          Delivery = deliveryType,
        });
      }
    }

    public void Broadcast(Span<byte> rawData, DeliveryType deliveryType = DeliveryType.Unreliable)
    {
      var bytes = rawData.ToArray();
      foreach (var player in _players.Values.ToArray())
      {
        _roomThread.WriteOutEvent(new Event()
        {
          PeerId = player.PeerId,
          Data = bytes,
          Type = EventType.DATA,
          Delivery = deliveryType,
        });
      }
    }
  }
}