using System;
using System.Collections.Generic;
using NLog;
using Ragon.Common;

namespace Ragon.Core
{
  public class PluginBase 
  {
    private delegate void SubscribeDelegate(Player player, ref ReadOnlySpan<byte> data);
    private delegate void SubscribeEntityDelegate(Player player, Entity entity, ref ReadOnlySpan<byte> data);

    private Dictionary<ushort, SubscribeDelegate> _globalEvents = new();
    private Dictionary<int, Dictionary<ushort, SubscribeEntityDelegate>> _entityEvents = new();
    private readonly RagonSerializer _serializer = new();

    protected IGameRoom Room { get; private set; } = null!;
    protected ILogger Logger = null!;

    public void Attach(GameRoom gameRoom)
    {
      Logger = LogManager.GetLogger($"Plugin<{GetType().Name}>");

      Room = gameRoom;

      _globalEvents.Clear();
      _entityEvents.Clear();
    }

    public void Detach()
    {
      _globalEvents.Clear();
      _entityEvents.Clear();
    }

    public void OnEvent<T>(ushort evntCode, Action<Player, T> action) where T : IRagonSerializable, new()
    {
      if (_globalEvents.ContainsKey(evntCode))
      {
        Logger.Warn($"Event subscriber already added {evntCode}");
        return;
      }

      var data = new T();
      _globalEvents.Add(evntCode, (Player player, ref ReadOnlySpan<byte> raw) =>
      {
        if (raw.Length == 0)
        {
          Logger.Warn($"Payload is empty for event {evntCode}");
          return;
        }

        _serializer.Clear();
        _serializer.FromSpan(ref raw);
        data.Deserialize(_serializer);
        action.Invoke(player, data);
      });
    }

    public void OnEvent(ushort evntCode, Action<Player> action)
    {
      if (_globalEvents.ContainsKey(evntCode))
      {
        Logger.Warn($"Event subscriber already added {evntCode}");
        return;
      }

      _globalEvents.Add(evntCode, (Player player, ref ReadOnlySpan<byte> raw) => { action.Invoke(player); });
    }

    public void OnEvent<T>(Entity entity, ushort evntCode, Action<Player, Entity, T> action) where T : IRagonSerializable, new()
    {
      if (_entityEvents.ContainsKey(entity.EntityId))
      {
        if (_entityEvents[entity.EntityId].ContainsKey(evntCode))
        {
          Logger.Warn($"Event subscriber already added {evntCode} for {entity.EntityId}");
          return;
        }

        var data = new T();
        _entityEvents[entity.EntityId].Add(evntCode, (Player player, Entity ent, ref ReadOnlySpan<byte> raw) =>
        {
          if (raw.Length == 0)
          {
            Logger.Warn($"Payload is empty for entity {ent.EntityId} event {evntCode}");
            return;
          }

          _serializer.Clear();
          _serializer.FromSpan(ref raw);
          data.Deserialize(_serializer);
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
            Logger.Warn($"Payload is empty for entity {ent.EntityId} event {evntCode}");
            return;
          }

          _serializer.Clear();
          _serializer.FromSpan(ref raw);
          data.Deserialize(_serializer);
          action.Invoke(player, ent, data);
        });
      }
    }

    public void OnEvent(Entity entity, ushort evntCode, Action<Player, Entity> action)
    {
      if (_entityEvents.ContainsKey(entity.EntityId))
      {
        if (_entityEvents[entity.EntityId].ContainsKey(evntCode))
        {
          Logger.Warn($"Event subscriber already added {evntCode} for {entity.EntityId}");
          return;
        }

        _entityEvents[entity.EntityId].Add(evntCode, (Player player, Entity ent, ref ReadOnlySpan<byte> raw) => { action.Invoke(player, ent); });
        return;
      }

      {
        _entityEvents.Add(entity.EntityId, new Dictionary<ushort, SubscribeEntityDelegate>());
        _entityEvents[entity.EntityId].Add(evntCode, (Player player, Entity ent, ref ReadOnlySpan<byte> raw) => { action.Invoke(player, ent); });
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

    public void ReplicateEvent(Player player, uint eventCode, IRagonSerializable payload)
    {
      _serializer.Clear();
      _serializer.WriteOperation(RagonOperation.REPLICATE_EVENT);
      
      payload.Serialize(_serializer);
      
      var sendData = _serializer.ToArray();
      Room.Send(player.PeerId, sendData);
    }

    public void ReplicateEvent(ushort eventCode, IRagonSerializable payload)
    {
      _serializer.Clear();
      _serializer.WriteOperation(RagonOperation.REPLICATE_EVENT);
      
      payload.Serialize(_serializer);

      var sendData = _serializer.ToArray();
      Room.Broadcast(sendData, DeliveryType.Reliable);
    }

    public void ReplicateEntityEvent(Player player, Entity entity, IRagonSerializable payload)
    {
      _serializer.Clear();
      _serializer.WriteOperation(RagonOperation.REPLICATE_ENTITY_EVENT);
      _serializer.WriteInt(entity.EntityId);

      payload.Serialize(_serializer);

      var sendData = _serializer.ToArray();
      Room.Send(player.PeerId, sendData, DeliveryType.Reliable);
    }

    public void ReplicateEntityEvent(Entity entity, IRagonSerializable payload)
    {
      _serializer.Clear();
      _serializer.WriteOperation(RagonOperation.REPLICATE_ENTITY_EVENT);
      _serializer.WriteInt(entity.EntityId);

      payload.Serialize(_serializer);

      var sendData = _serializer.ToArray();
      Room.Broadcast(sendData);
    }


    #region VIRTUAL

    public virtual void OnPlayerJoined(Player player)
    {
    }

    public virtual void OnPlayerLeaved(Player player)
    {
    }

    public virtual void OnOwnershipChanged(Player player)
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
    
    #endregion
  }
}