using System;
using System.Collections.Generic;
using System.Text;
using NetStack.Serialization;
using NLog;
using Ragon.Common.Protocol;

namespace Ragon.Core
{
  public class RoomManager
  {
    private readonly Logger _logger = LogManager.GetCurrentClassLogger();
    private List<Room> _rooms;
    private Dictionary<uint, Room> _peersByRoom;
    private PluginFactory _factory;
    private RoomThread _roomThread;
    private BitBuffer _bitBuffer;

    public Action<(uint, Room)> OnJoined;
    public Action<(uint, Room)> OnLeaved;

    public RoomManager(RoomThread roomThread, PluginFactory factory)
    {
      _roomThread = roomThread;
      _factory = factory;
      _rooms = new List<Room>();
      _peersByRoom = new Dictionary<uint, Room>();
      _bitBuffer = new BitBuffer(1024);
    }

    public void ProccessEvent(RagonOperation operation, uint peerId, byte[] payload)
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

    public void OnAuthorize(uint peerId, byte[] payload)
    {
      _bitBuffer.Clear();
      // _bitBuffer.FromArray(payload, payload.Length);

      // var authorizePacket = new AuthorationData();
      // authorizePacket.Deserialize(_bitBuffer);

      var data = new byte[2];
      
      ProtocolHeader.WriteOperation((ushort) RagonOperation.AUTHORIZED_SUCCESS, data);

      _roomThread.WriteOutEvent(new Event()
      {
        Type = EventType.DATA,
        Data = data,
        PeerId = peerId,
      });
    }

    public Room Join(uint peerId, byte[] payload)
    {
      var map = Encoding.UTF8.GetString(payload);
      
      if (_rooms.Count > 0)
      {
        var existsRoom = _rooms[0];
        existsRoom.Joined(peerId, payload);
        _peersByRoom.Add(peerId, existsRoom);
        
        return existsRoom;
      }

      var plugin = _factory.CreatePlugin(map);
      if (plugin == null)
        throw new NullReferenceException($"Plugin for map {map} is null");

      _logger.Info("Room created");

      var room = new Room(_roomThread, plugin, map);
      room.Joined(peerId, payload);
      _peersByRoom.Add(peerId, room);

      _rooms.Add(room);

      return room;
    }

    public Room Left(uint peerId, byte[] payload)
    {
      _peersByRoom.Remove(peerId, out var room);
      room?.Leave(peerId);

      return room;
    }

    public void Tick(float deltaTime)
    {
      foreach (Room room in _rooms)
        room.Tick(deltaTime);
    }
  }
}