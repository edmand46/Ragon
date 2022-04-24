
using System;
using System.Collections.Generic;

namespace Ragon.Core
{
    public class PluginBase
    {
        static class Storage<T>
        {
            public static Dictionary<Room, Dictionary<ushort, Action<T>>> Subscribes = new();    
        }
        
        
        protected Room _room;
        // protected Dictionary<ushort, > _subscribes = new Dictionary<ushort,???>(); 
        public void Attach(Room room) => _room = room;

        public void Subscribe<T>(ushort evntCode, Action<T> val)
        {
            Storage<T>.Subscribes.Add(_room, val);
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
    }
}