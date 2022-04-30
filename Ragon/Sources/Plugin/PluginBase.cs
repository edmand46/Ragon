
using System;
using System.Collections.Generic;
using NetStack.Serialization;
using Ragon.Common.Protocol;

namespace Ragon.Core
{
    public class PluginBase
    {
        private delegate void SubscribeDelegate(Player player, ref ReadOnlySpan<byte> data);
        private Dictionary<ushort, SubscribeDelegate> _subscribes = new();
        private BitBuffer _buffer = new BitBuffer(1024);
        
        protected Room Room { get; private set; }
        
        public void Attach(Room room) => Room = room;
        public void Detach() => _subscribes.Clear();
        
        public void Subscribe<T>(ushort evntCode, Action<Player, T> action) where T: IPacket, new()
        {
            var data = new T();
            _subscribes.Add(evntCode, (Player player, ref ReadOnlySpan<byte> raw) =>
            {
                _buffer.Clear();
                _buffer.FromSpan(ref raw, raw.Length);
                data.Deserialize(_buffer);
                action.Invoke(player, data);
            });
        }
        
        // public void Subscribe<T>(RagonOperation operation, Action<Player, Span<byte> action) where T: IPacket, new()
        // {
        //     var data = new T();
        //     _subscribes.Add(evntCode, (Player player, ref ReadOnlySpan<byte> raw) =>
        //     {
        //         _buffer.Clear();
        //         _buffer.FromSpan(ref raw, raw.Length);
        //         data.Deserialize(_buffer);
        //         action.Invoke(player, data);
        //     });
        // }
        public void UnsubscribeAll()
        {
            _subscribes.Clear();
        }


        public bool InternalHandle(uint peerId, ushort evntCode, ref ReadOnlySpan<byte> payload)
        {
            if (_subscribes.ContainsKey(evntCode))
            {
                var player = Room.GetPlayerByPeerId(peerId);
                _subscribes[evntCode].Invoke(player, ref payload);
                return true;
            }

            return false;
        }

        public void Send(Player player, RagonOperation operation, IPacket payload)
        {
            Send(player.PeerId, operation, payload);
        }

        public void Broadcast(Player[] players, RagonOperation operation, IPacket payload)
        {
            var ids = new uint[players.Length];
            for (int i = 0; i < players.Length; i++)
                ids[i] = players[i].PeerId;
            
            Broadcast(ids, operation, payload);
        }
        
        public void Send(uint peerId, RagonOperation operation, IPacket payload)
        {
            _buffer.Clear();
            
            payload.Serialize(_buffer);
            
            Span<byte> data = stackalloc byte[_buffer.Length + 2];
            Span<byte> bufferSpan = data.Slice(2, data.Length - 2);
            
            _buffer.ToSpan(ref bufferSpan);

            RagonHeader.WriteUShort((ushort) operation, ref data);
            
            Room.Send(peerId, data);
        }


        public void Broadcast(uint[] peersIds, RagonOperation operation, IPacket payload)
        {
            _buffer.Clear();
            payload.Serialize(_buffer);

            Span<byte> data = stackalloc byte[_buffer.Length + 2];
            Span<byte> bufferSpan = data.Slice(2, data.Length - 2);

            _buffer.ToSpan(ref bufferSpan);

            RagonHeader.WriteUShort((ushort) operation, ref data);

            Room.Broadcast(peersIds, data);
        }

        #region VIRTUAL
        
        public virtual void OnRoomJoined()
        {
            
        }

        public virtual void OnRoomLeaved()
        {
            
        }
        
        public virtual void OnStart()
        {
        }

        public virtual void OnStop()
        {
        }

        public virtual void OnTick(ulong ticks, float deltaTime)
        {
            
        }

        #endregion
    }
}