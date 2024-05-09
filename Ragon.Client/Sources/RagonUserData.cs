/*
 * Copyright 2024 Eduard Kargin <kargin.eduard@gmail.com>
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

namespace Ragon.Client
{
  public class RagonUserData: IUserData
  {
    public byte[] this[string key]
    {
      get => _properties[key];
      set
      {
        _properties[key] = value;

        if (!_changesCache.ContainsKey(key))
        {
          _localChanges.Add(key);
          _changesCache.Add(key, true);
        }
      }
    }
    
    public void Remove(string key)
    {
      if (_properties.Remove(key))
      {
        if (!_changesCache.ContainsKey(key))
        {
          _localChanges.Add(key);
          _changesCache.Add(key, true);
        }
      }
    }
    
    public bool Dirty => _localChanges.Count > 0;

    private readonly List<string> _localChanges = new ();
    private readonly Dictionary<string, bool> _changesCache = new();
    private readonly Dictionary<string, byte[]> _properties = new();
    
    public RagonUserData()
    {
     
    }

    public IReadOnlyList<string> Read(RagonBuffer buffer)
    {
      var len = buffer.ReadUShort();
      var changes = new List<string>(len);
      for (int i = 0; i < len; i++)
      {
        var key = buffer.ReadString();
        var valueSize = buffer.ReadUShort();  
        if (valueSize > 0)
        {
          var value = buffer.ReadBytes(valueSize);
          _properties[key] = value;
          
        }
        else
        {
          _properties.Remove(key);
        }
        
        changes.Add(key);
      }

      return changes;
    }

    public void Write(RagonBuffer buffer)
    {
      buffer.WriteUShort((ushort)_localChanges.Count);
      foreach (var propertyChanged in _localChanges)
      {
        buffer.WriteString(propertyChanged);
        if (_properties.TryGetValue(propertyChanged, out var property))
        {
          buffer.WriteUShort((ushort) property.Length);
          buffer.WriteBytes(property);          
        }
        else
        {
          buffer.WriteUShort(0);
        }
      }
      
      _localChanges.Clear();
    }
  }
}