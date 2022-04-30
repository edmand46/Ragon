using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using DisruptorUnity3d;

namespace Ragon.Core;

public class EventStream
{
  private Stack<Event> _pool = new Stack<Event>(1024);
  private RingBuffer<Event> _events = new RingBuffer<Event>(1024);

  // public ref Event Reserve()
  // {
  //   if (_pool.Count == 0)
  //   {
  //     var evnt = new Event();
  //     return ref evnt;
  //   }
  //   // var evnt = _pool.Pop();
  //   // ref Event evntRef = ref evnt;
  //   // return evntRef;
  // }

  // public void Retain(ref Event @event)
  // {
  //   
  // }
  //
  // public void WriteEvent(ref Event evnt)
  // {
  //   // _pool.Push(evnt);
  //   _events.Enqueue(evnt);
  // }
  //
  // public ref Event ReadEvent()
  // {
  //   
  // }
}