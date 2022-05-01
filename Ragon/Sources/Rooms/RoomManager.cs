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

    public Action<(uint, Room)> OnJoined;
    public Action<(uint, Room)> OnLeaved;

    public RoomManager(RoomThread roomThread, PluginFactory factory)
    {
      _roomThread = roomThread;
      _factory = factory;

      _manager = _factory.CreateManager(roomThread.Configuration);
      _rooms = new List<Room>();
      _peersByRoom = new Dictionary<uint, Room>();
    }

    public void ProcessEvent(RagonOperation operation, uint peerId, byte[] payload)
    {
      switch (operation)
      {
        case RagonOperation.AUTHORIZE:
        {
          OnAuthorize(peerId, payload);
          break;
        }
        case RagonOperation.JOIN_ROOM:
        {
          var room = Join(peerId, payload);
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
        Span<byte> data =  stackalloc byte[2];
        RagonHeader.WriteUShort((ushort) RagonOperation.AUTHORIZED_SUCCESS, ref data);

        var bytes = data.ToArray();
        _roomThread.WriteOutEvent(new Event()
        {
          Delivery = DeliveryType.Reliable,
          Type = EventType.DATA,
          Data = bytes,
          PeerId = peerId,
        });  
      }
      else
      {
        Span<byte> data =  stackalloc byte[2];
        RagonHeader.WriteUShort((ushort) RagonOperation.AUTHORIZED_FAILED, ref data);

        var bytes = data.ToArray();
        _roomThread.WriteOutEvent(new Event()
        {
          Delivery = DeliveryType.Reliable,
          Type = EventType.DATA,
          Data = bytes,
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

    public Room Join(uint peerId, ReadOnlySpan<byte> payload)
    {
      var minData = payload.Slice(0, 2);
      var maxData = payload.Slice(2, 2);
      var mapData = payload.Slice(4, payload.Length - 4);
      
      var map = Encoding.UTF8.GetString(mapData);
      var min = RagonHeader.ReadUShort(ref minData);
      var max = RagonHeader.ReadUShort(ref maxData);
      
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

    public Room Left(uint peerId, byte[] payload)
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
        if (room.PlayersCount <= 0)
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