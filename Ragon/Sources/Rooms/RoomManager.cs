using System;
using System.Collections.Generic;
using System.Text;
using NLog;
using Ragon.Common;

namespace Ragon.Core
{
  public class RoomManager
  {
    private readonly Logger _logger = LogManager.GetCurrentClassLogger();
    private List<Room> _rooms;
    private Dictionary<uint, Room> _peersByRoom;
    private PluginFactory _factory;
    private AuthorizationManager _manager;
    private RoomThread _roomThread;
    private RagonSerializer _serializer;
    public Action<(uint, Room)> OnJoined;
    public Action<(uint, Room)> OnLeaved;

    public RoomManager(RoomThread roomThread, PluginFactory factory)
    {
      _roomThread = roomThread;
      _factory = factory;

      _serializer = new RagonSerializer();
      _manager = _factory.CreateManager(roomThread.Configuration);
      _rooms = new List<Room>();
      _peersByRoom = new Dictionary<uint, Room>();
    }

    public void ProcessEvent(RagonOperation operation, uint peerId, ReadOnlySpan<byte> payload)
    {
      switch (operation)
      {
        case RagonOperation.AUTHORIZE:
        {
          OnAuthorize(peerId, payload);
          break;
        }
        case RagonOperation.JOIN_OR_CREATE_ROOM:
        {
          var room = JoinOrCreate(peerId, payload);
          if (room == null)
          {
            var sendData = new[] {(byte) RagonOperation.JOIN_FAILED};
            _roomThread.WriteOutEvent(new Event()
            {
              Delivery = DeliveryType.Reliable,
              Type = EventType.DATA,
              Data = sendData,
              PeerId = peerId,
            });
            return;
          }
          OnJoined?.Invoke((peerId, room));
          break;
        }
        case RagonOperation.JOIN_ROOM:
        {
          var room = Join(peerId, payload);
          if (room == null)
          {
            var sendData = new[] {(byte) RagonOperation.JOIN_FAILED};
            _roomThread.WriteOutEvent(new Event()
            {
              Delivery = DeliveryType.Reliable,
              Type = EventType.DATA,
              Data = sendData,
              PeerId = peerId,
            });
            
            return;
          }
          OnJoined?.Invoke((peerId, room));
          break;
        }
        case RagonOperation.LEAVE_ROOM:
        {
          var room = Left(peerId, payload);
          OnLeaved((peerId, room));
          break;
        }
      }
    }

    public void OnAuthorize(uint peerId, ReadOnlySpan<byte> payload)
    {
      if (_manager.OnAuthorize(peerId, ref payload))
      {
        var sendData = new[] {(byte) RagonOperation.AUTHORIZED_SUCCESS};
        _roomThread.WriteOutEvent(new Event()
        {
          Delivery = DeliveryType.Reliable,
          Type = EventType.DATA,
          Data = sendData,
          PeerId = peerId,
        });
      }
      else
      {
        var sendData = new[] {(byte) RagonOperation.AUTHORIZED_FAILED};
        _roomThread.WriteOutEvent(new Event()
        {
          Delivery = DeliveryType.Reliable,
          Type = EventType.DATA,
          Data = sendData,
          PeerId = peerId,
        });
        _roomThread.WriteOutEvent(new Event()
        {
          Delivery = DeliveryType.Reliable,
          Type = EventType.DISCONNECTED,
          Data = Array.Empty<byte>(),
          PeerId = peerId,
        });
      }
    }

    public Room? Join(uint peerId, ReadOnlySpan<byte> payload)
    {
      var roomId = Encoding.UTF8.GetString(payload);
      
      if (_rooms.Count > 0)
      {
        foreach (var existRoom in _rooms)
        {
          if (existRoom.Id == roomId && existRoom.PlayersCount < existRoom.PlayersMax)
          {
            existRoom.Joined(peerId, payload);

            _peersByRoom.Add(peerId, existRoom);

            return existRoom;
          }
        }
      }

      return null;
    }

    public Room? JoinOrCreate(uint peerId, ReadOnlySpan<byte> payload)
    {
      _serializer.Clear();
      _serializer.FromSpan(ref payload);

      var min = _serializer.ReadUShort();
      var max = _serializer.ReadUShort();
      var map = _serializer.ReadString();
      
      Room room = null;
      if (_rooms.Count > 0)
      {
        foreach (var existRoom in _rooms)
        {
          if (existRoom.Map == map && existRoom.PlayersCount < existRoom.PlayersMax)
          {
            room = existRoom;
            room.Joined(peerId, payload);

            _peersByRoom.Add(peerId, room);

            return room;
          }
        }
      }

      var plugin = _factory.CreatePlugin(map);
      if (plugin == null)
        throw new NullReferenceException($"Plugin for map {map} is null");

      room = new Room(_roomThread, plugin, map, min, max);
      room.Joined(peerId, payload);
      room.Start();

      _peersByRoom.Add(peerId, room);
      _rooms.Add(room);

      return room;
    }

    public Room Left(uint peerId, ReadOnlySpan<byte> payload)
    {
      _peersByRoom.Remove(peerId, out var room);

      return room;
    }

    public void Disconnected(uint peerId)
    {
      _peersByRoom.Remove(peerId, out var room);
      if (room != null)
      {
        room.Leave(peerId);
        if (room.PlayersCount <= 0 && room.PlayersMin > 0)
        {
          _rooms.Remove(room);

          room.Stop();
          room.Dispose();
        }
      }
    }

    public void Tick(float deltaTime)
    {
      foreach (Room room in _rooms)
        room.Tick(deltaTime);
    }
  }
}