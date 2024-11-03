/*
 * Copyright 2023-2024 Eduard Kargin <kargin.eduard@gmail.com>
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

namespace Ragon.Client;

public class RagonEventCache
{
  private readonly Dictionary<Type, ushort> _eventsRegistryByType = new();
  private readonly HashSet<ushort> _codes = new();
  private readonly HashSet<Type> _types = new();
  private ushort _eventIdGenerator = 0;

  public ushort GetEventCode<TEvent>(TEvent _) where TEvent : IRagonEvent
  {
    var type = typeof(TEvent);
   
    if (!_eventsRegistryByType.TryGetValue(type, out var eventCode))
    {
      RagonLog.Error($"Event with type {type} not registered");
      return 0;
    }
    
    return eventCode;
  }

  public void Register<T>() where T : IRagonEvent, new()
  {
    var type = typeof(T);
    if (_types.Contains(type))
    {
      RagonLog.Trace($"[Ragon] Event already registered: {type.Name}");
      return;
    }

    RagonLog.Trace($"[Ragon] Registered Event: {type.Name} - {_eventIdGenerator}");

    _eventsRegistryByType.Add(type, _eventIdGenerator);
    _codes.Add(_eventIdGenerator);
    _types.Add(type);
    
    _eventIdGenerator++;
  }

  public void Register<T>(ushort evntCode) where T : IRagonEvent, new()
  {
    var type = typeof(T);
    if (_codes.Contains(evntCode) || _types.Contains(type))
    {
      RagonLog.Warn($"[Ragon] Event already registered: {type.Name} - {evntCode}");
      return;
    }

    RagonLog.Trace($"[Ragon] Registered Event: {type.Name} - {evntCode}");

    _codes.Add(evntCode);
    _types.Add(type);
    _eventsRegistryByType.Add(type, evntCode);
  }

  public T Create<T>() where T : IRagonEvent, new()
  {
    return new T();
  }
}