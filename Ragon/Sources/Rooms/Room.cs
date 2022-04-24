using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NetStack.Serialization;
using NLog;
using Ragon.Common.Protocol;

namespace Ragon.Core
{
  public class Room : IDisposable
  {
    private ILogger _logger = LogManager.GetCurrentClassLogger();
    private Dictionary<uint, Player> _players = new();
    private Dictionary<int, Entity> _entities = new();
    private uint Owner;

    private BitBuffer _buffer = new BitBuffer(8192);
    private byte[] _bytes = new byte[8192];

    private readonly PluginBase _plugin;
    private readonly RoomThread _roomThread;
    private readonly string _map;
    private ulong _ticks = 0;

    // Cache
    private uint[] _readyPlayers = Array.Empty<uint>();

    public int Players => _players.Count;
    public int MaxPlayers { get; } = 0;

    public Room(RoomThread roomThread, PluginBase pluginBase, string map)
    {
      _roomThread = roomThread;
      _plugin = pluginBase;
      _map = map;

      _plugin.Attach(this);
    }

    public void Joined(uint peerId, byte[] payload)
    {
      if (_players.Count == 0)
      {
        Owner = peerId;
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
      _plugin.OnPlayerConnected(player);

      var data = new byte[8];
      ProtocolHeader.WriteEntity((int) peerId, data, 0);
      ProtocolHeader.WriteEntity((int) Owner, data, 4);
      Send(peerId, RagonOperation.JOIN_ROOM, data);

      var sceneRawData = Encoding.UTF8.GetBytes(_map);
      Send(peerId, RagonOperation.LOAD_SCENE, sceneRawData);
    }

    public void Leave(uint peerId)
    {
      if (_players.Remove(peerId, out var player))
      {
        _plugin.OnPlayerDisconnected(player);
        foreach (var entityId in player.EntitiesIds)
        {
          var entityData = new byte[4];
          ProtocolHeader.WriteEntity(entityId, entityData, 0);
          Broadcast(_readyPlayers, RagonOperation.DESTROY_ENTITY, entityData);
          
          _entities.Remove(entityId);
        }
      }
    }

    public void ProcessEvent(RagonOperation operation, uint peerId, byte[] rawData)
    {
      switch (operation)
      {
        case RagonOperation.REPLICATE_ENTITY_STATE:
        {
          var entityId = ProtocolHeader.ReadEntity(rawData, 2);
          if (_entities.TryGetValue(entityId, out var ent))
          {
            var data = new byte[rawData.Length - 6]; // opcode(ushort)(2) + entity(int)(4) 
            Array.Copy(rawData, 6, data, 0, rawData.Length - 6);
            _entities[entityId].State = data;

            Broadcast(_readyPlayers, rawData);
          }

          break;
        }
        case RagonOperation.REPLICATE_ENTITY_PROPERTY:
        {
          var entityId = ProtocolHeader.ReadEntity(rawData, 2);
          if (_entities.TryGetValue(entityId, out var ent))
          {
            var propertyId = ProtocolHeader.ReadProperty(rawData, 6);
            var data = new byte[rawData.Length - 10]; // opcode(ushort)(2) + entity(int)(4) + propertyId(int)(4) 
            Array.Copy(rawData, 10, data, 0, rawData.Length - 10);

            var props = _entities[entityId].Properties;
            if (props.ContainsKey(propertyId))
            {
              props[propertyId] = data;
            }
            else
            {
              props.Add(propertyId, data);
            }

            Broadcast(_readyPlayers, RagonOperation.REPLICATE_ENTITY_PROPERTY, rawData);
          }

          break;
        }
        case RagonOperation.REPLICATE_EVENT:
        {
          
          Broadcast(_readyPlayers, rawData);
          break;
        }
        case RagonOperation.CREATE_ENTITY:
        {
          var entity = new Entity(peerId);
          var data = new byte[rawData.Length - 2]; // opcode(ushort)(2) 
          Array.Copy(rawData, 2, data, 0, rawData.Length - 2);

          entity.State = data;
          entity.Properties = new Dictionary<int, byte[]>();

          var player = _players[peerId];
          player.Entities.Add(entity);
          player.EntitiesIds.Add(entity.EntityId);

          _entities.Add(entity.EntityId, entity);

          var entityData = new byte[entity.State.Length + 8];
          ProtocolHeader.WriteEntity(entity.EntityId, entityData, 0);
          ProtocolHeader.WriteEntity((int) peerId, entityData, 4);
          
          Array.Copy(entity.State, 0, entityData, 8, entity.State.Length);
          
          _logger.Trace("Create entity Owner:" + peerId + " Id: " + entity.EntityId);
          
          Broadcast(_readyPlayers, RagonOperation.CREATE_ENTITY, entityData);
          break;
        }
        case RagonOperation.DESTROY_ENTITY:
        {
          var entityId = ProtocolHeader.ReadEntity(rawData);
          if (_entities.TryGetValue(entityId, out var entity))
          {
            if (entity.OwnerId == peerId)
            {
              var player = _players[peerId];

              player.Entities.Remove(entity);
              player.EntitiesIds.Remove(entity.EntityId);

              _entities.Remove(entityId);

              Broadcast(_readyPlayers, rawData);
            }
          }

          break;
        }
        case RagonOperation.SCENE_IS_LOADED:
        {
          Send(peerId, RagonOperation.RESTORE_BEGIN, Array.Empty<byte>());
          foreach (var entity in _entities.Values)
          {
            var entityData = new byte[entity.State.Length + 8];
            ProtocolHeader.WriteEntity(entity.EntityId, entityData, 0);
            ProtocolHeader.WriteEntity((int) entity.OwnerId, entityData, 4);
          
            Array.Copy(entity.State, 0, entityData, 8, entity.State.Length);
            Send(peerId, RagonOperation.CREATE_ENTITY, entityData);
          }
          Send(peerId, RagonOperation.RESTORE_END, Array.Empty<byte>());
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
    }

    public void Start()
    {
      _plugin.OnStart();
    }

    public void Stop()
    {
      _plugin.OnStop();
    }

    public void Dispose()
    {
    }

    public void Send(uint peerId, RagonOperation operation, byte[] payload)
    {
      if (payload.Length > 0)
      {
        var data = new byte[payload.Length + 2];

        Array.Copy(payload, 0, data, 2, payload.Length);
        ProtocolHeader.WriteOperation((ushort) operation, data);

        _roomThread.WriteOutEvent(new Event()
        {
          PeerId = peerId,
          Data = data,
          Type = EventType.DATA,
        });
      }
      else
      {
        var data = new byte[2];

        ProtocolHeader.WriteOperation((ushort) operation, data);

        _roomThread.WriteOutEvent(new Event()
        {
          PeerId = peerId,
          Data = data,
          Type = EventType.DATA,
        });
      }
    }

    public void Send(uint peerId, RagonOperation operation, IData payload)
    {
      _buffer.Clear();
      payload.Serialize(_buffer);
      _buffer.ToArray(_bytes);

      var data = new byte[_buffer.Length + 2];

      Array.Copy(_bytes, 0, data, 2, _buffer.Length);

      ProtocolHeader.WriteOperation((ushort) operation, data);

      _roomThread.WriteOutEvent(new Event()
      {
        PeerId = peerId,
        Data = data,
        Type = EventType.DATA,
      });
    }


    public void Broadcast(uint[] peersIds, RagonOperation operation, IData payload)
    {
      _buffer.Clear();
      payload.Serialize(_buffer);
      _buffer.ToArray(_bytes);

      var data = new byte[_buffer.Length + 2];

      Array.Copy(_bytes, 0, data, 2, _buffer.Length);

      ProtocolHeader.WriteOperation((ushort) operation, data);

      foreach (var peer in peersIds)
      {
        _roomThread.WriteOutEvent(new Event()
        {
          PeerId = peer,
          Data = data,
          Type = EventType.DATA,
        });
      }
    }

    public void Broadcast(uint[] peersIds, RagonOperation operation, byte[] payload)
    {
      var data = new byte[payload.Length + 2];

      Array.Copy(payload, 0, data, 2, payload.Length);

      ProtocolHeader.WriteOperation((ushort) operation, data);

      foreach (var peer in peersIds)
      {
        _roomThread.WriteOutEvent(new Event()
        {
          PeerId = peer,
          Data = data,
          Type = EventType.DATA,
        });
      }
    }

    public void Broadcast(uint[] peersIds, byte[] rawData)
    {
      foreach (var peer in peersIds)
      {
        _roomThread.WriteOutEvent(new Event()
        {
          PeerId = peer,
          Data = rawData,
          Type = EventType.DATA,
        });
      }
    }

    public void Broadcast(byte[] rawData)
    {
      foreach (var player in _players.Values.ToArray())
      {
        _roomThread.WriteOutEvent(new Event()
        {
          PeerId = player.PeerId,
          Data = rawData,
          Type = EventType.DATA,
        });
      }
    }
  }
}