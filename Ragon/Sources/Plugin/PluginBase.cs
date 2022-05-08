using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using NetStack.Serialization;
using NLog;
using Ragon.Common;

namespace Ragon.Core
{
  public class PluginBase: IDisposable
  {
    private delegate void SubscribeDelegate(Player player, ref ReadOnlySpan<byte> data);
    private delegate void SubscribeEntityDelegate(Player player, Entity entity, ref ReadOnlySpan<byte> data);

    private Dictionary<ushort, SubscribeDelegate> _globalEvents = new();
    private Dictionary<int, Dictionary<ushort, SubscribeEntityDelegate>> _entityEvents = new();
    private BitBuffer _buffer = new BitBuffer(8192);

    protected Room Room { get; private set; }
    protected ILogger _logger;
    
    public void Attach(Room room)
    {
      _logger = LogManager.GetLogger($"Plugin<{GetType().Name}>");
      
      Room = room;
      
      _globalEvents.Clear();
      _entityEvents.Clear();
    }
    public void Dispose()
    {
      _globalEvents.Clear();
      _entityEvents.Clear();
    }
    
    public void Subscribe<T>(ushort evntCode, Action<Player, T> action) where T : IRagonSerializable, new()
    {
      if (_globalEvents.ContainsKey(evntCode))
      {
        _logger.Warn($"Event subscriber already added {evntCode}");
        return;
      }
      
      var data = new T();
      _globalEvents.Add(evntCode, (Player player, ref ReadOnlySpan<byte> raw) =>
      {
        if (raw.Length == 0)
        {
          _logger.Warn($"Payload is empty for event {evntCode}");
          return;
        }
        
        _buffer.Clear();
        _buffer.FromSpan(ref raw, raw.Length);
        data.Deserialize(_buffer);
        action.Invoke(player, data);
      });
    }
    
    public void Subscribe(ushort evntCode, Action<Player> action)
    {
      if (_globalEvents.ContainsKey(evntCode))
      {
        _logger.Warn($"Event subscriber already added {evntCode}");
        return;
      }
      
      _globalEvents.Add(evntCode, (Player player, ref ReadOnlySpan<byte> raw) =>
      {
        action.Invoke(player);
      });
    }

    public void Subscribe<T>(Entity entity, ushort evntCode, Action<Player, Entity, T> action) where T : IRagonSerializable, new()
    {
      if (_entityEvents.ContainsKey(entity.EntityId))
      {
        if (_entityEvents[entity.EntityId].ContainsKey(evntCode))
        {
          _logger.Warn($"Event subscriber already added {evntCode} for {entity.EntityId}");
          return;
        }   
        
        var data = new T();
        _entityEvents[entity.EntityId].Add(evntCode, (Player player, Entity ent, ref ReadOnlySpan<byte> raw) =>
        {
          if (raw.Length == 0)
          {
            _logger.Warn($"Payload is empty for entity {ent.EntityId} event {evntCode}");
            return;
          }
          
          _buffer.Clear();
          _buffer.FromSpan(ref raw, raw.Length);
          data.Deserialize(_buffer);
          action.Invoke(player, ent, data);
        });
        
        return;
      }

      {
        var data = new T();
        _entityEvents.Add(entity.EntityId, new Dictionary<ushort, SubscribeEntityDelegate>());
        _entityEvents[entity.EntityId].Add(evntCode, (Player player, Entity ent, ref ReadOnlySpan<byte> raw) =>
        {
          if (raw.Length == 0)
          {
            _logger.Warn($"Payload is empty for entity {ent.EntityId} event {evntCode}");
            return;
          }
          _buffer.Clear();
          _buffer.FromSpan(ref raw, raw.Length);
          data.Deserialize(_buffer);
          action.Invoke(player, ent, data);
        });
      }
    }

    public void Subscribe(Entity entity, ushort evntCode, Action<Player, Entity> action)
    {
      if (_entityEvents.ContainsKey(entity.EntityId))
      {
        if (_entityEvents[entity.EntityId].ContainsKey(evntCode))
        {
          _logger.Warn($"Event subscriber already added {evntCode} for {entity.EntityId}");
          return;
        }   
        
        _entityEvents[entity.EntityId].Add(evntCode, (Player player, Entity ent, ref ReadOnlySpan<byte> raw) =>
        {
          action.Invoke(player, ent);
        });
        return;
      }

      {
        _entityEvents.Add(entity.EntityId, new Dictionary<ushort, SubscribeEntityDelegate>());
        _entityEvents[entity.EntityId].Add(evntCode, (Player player, Entity ent, ref ReadOnlySpan<byte> raw) =>
        {
          action.Invoke(player, ent);
        });
      }
    }
    
    public void UnsubscribeAll()
    {
      _globalEvents.Clear();
      _entityEvents.Clear();
    }

    public bool InternalHandle(uint peerId, int entityId, ushort evntCode, ref ReadOnlySpan<byte> payload)
    {
      if (!_entityEvents.ContainsKey(entityId))
        return false;

      if (!_entityEvents[entityId].ContainsKey(evntCode))
        return false;
      
      var player = Room.GetPlayerById(peerId);
      var entity = Room.GetEntityById(entityId);
      _entityEvents[entityId][evntCode].Invoke(player, entity, ref payload);

      return true;
    }

    public bool InternalHandle(uint peerId, ushort evntCode, ref ReadOnlySpan<byte> payload)
    {
      if (_globalEvents.ContainsKey(evntCode))
      {
        var player = Room.GetPlayerById(peerId);
        _globalEvents[evntCode].Invoke(player, ref payload);
        return true;
      }

      return false;
    }

    public void SendEvent(Player player, uint eventCode, IRagonSerializable payload)
    {
      _buffer.Clear();
      payload.Serialize(_buffer);

      var sendData = new byte[_buffer.Length + 4];
      Span<byte> data = sendData.AsSpan();
      Span<byte> operationData = data.Slice(0, 2);
      Span<byte> eventCodeData = data.Slice(2, 2);
      Span<byte> payloadData = data.Slice(4, data.Length - 4);

      _buffer.ToSpan(ref payloadData);
      
      RagonHeader.WriteUShort((ushort) RagonOperation.REPLICATE_EVENT, ref operationData);
      RagonHeader.WriteUShort((ushort) eventCode, ref eventCodeData);
      
      Room.Send(player.PeerId, sendData);
    }
    
    public void SendEvent(ushort eventCode, IRagonSerializable payload)
    {
      _buffer.Clear();
      payload.Serialize(_buffer);

      var sendData = new byte[_buffer.Length + 4];
      Span<byte> data = sendData.AsSpan();
      Span<byte> operationData = data.Slice(0, 2);
      Span<byte> eventCodeData = data.Slice(2, 2);
      Span<byte> payloadData = data.Slice(4, _buffer.Length);
      
      RagonHeader.WriteUShort((ushort) RagonOperation.REPLICATE_EVENT,ref operationData);
      RagonHeader.WriteUShort( eventCode, ref eventCodeData);

      _buffer.ToSpan(ref payloadData);
      
      Room.Broadcast(sendData);
    }
    
    public void SendEntityEvent(Player player, Entity entity, IRagonSerializable payload)
    {
      _buffer.Clear();
      payload.Serialize(_buffer);

      var sendData = new byte[_buffer.Length + 6];
      Span<byte> data = sendData.AsSpan();
      Span<byte> operationData = data.Slice(0, 2);
      Span<byte> entityData = data.Slice(2, 4);
      
      RagonHeader.WriteUShort((ushort) RagonOperation.REPLICATE_EVENT, ref operationData);
      RagonHeader.WriteInt(entity.EntityId, ref entityData);
      
      Room.Send(player.PeerId, sendData);
    }
     
    public void SendEntityEvent(Entity entity, IRagonSerializable payload)
    {
      _buffer.Clear();
      payload.Serialize(_buffer);

      var sendData = new byte[_buffer.Length + 6];
      Span<byte> data = sendData.AsSpan();
      Span<byte> operationData = data.Slice(0, 2);
      Span<byte> entityData = data.Slice(2, 4);
      
      RagonHeader.WriteUShort((ushort) RagonOperation.REPLICATE_EVENT, ref operationData);
      RagonHeader.WriteInt(entity.EntityId, ref entityData);
      
      Room.Broadcast(sendData);
    }

    
    #region VIRTUAL

    public virtual void OnPlayerJoined(Player player)
    {
    }

    public virtual void OnPlayerLeaved(Player player)
    {
    }

    public virtual void OnOwnerChanged(Player player)
    {
      
    }
    public virtual void OnEntityCreated(Player creator, Entity entity)
    {
      
    }

    public virtual void OnEntityDestroyed(Player destoyer, Entity entity)
    {
      
    }

    public virtual void OnStart()
    {
    }

    public virtual void OnStop()
    {
    }

    public virtual void OnTick(float deltaTime)
    {
    }

    #endregion
  }
}