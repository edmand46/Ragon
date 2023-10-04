/*
 * Copyright 2023 Eduard Kargin <kargin.eduard@gmail.com>
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using Ragon.Protocol;
using Ragon.Server.Room;

namespace Ragon.Server.Event;

public class RagonEvent
{
  public RagonRoomPlayer Invoker { get; private set; }
  public ushort EventCode { get; private set; }
  public ushort Size => (ushort) _size;
  
  private uint[] _data = new uint[128];
  private int _size = 0;

  public RagonEvent(
    RagonRoomPlayer invoker,
    ushort eventCode
  )
  {
    Invoker = invoker;
    EventCode = eventCode;
  }
  
  public void Read(RagonBuffer buffer)
  {
    _size = buffer.Capacity;
    buffer.ReadArray(_data, _size);
  }

  public void Write(RagonBuffer buffer)
  {
    if (_size == 0) return;
    buffer.WriteArray(_data, _size);
  }
}