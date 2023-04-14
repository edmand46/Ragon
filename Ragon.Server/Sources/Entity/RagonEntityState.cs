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

namespace Ragon.Server.Entity;

public class RagonEntityState: IRagonEntityState
{
  private List<RagonProperty> _properties;
  private RagonEntity _entity;
  private RagonBuffer _buffer;
  
  public RagonEntityState(RagonEntity entity, int capacity = 10)
  {
    _entity = entity;
    _buffer = new RagonBuffer(8);
    _properties = new List<RagonProperty>(capacity);
  }

  public void AddProperty(RagonProperty property)
  {
    _properties.Add(property);
  }

  public void Write(RagonBuffer buffer)
  {
    buffer.WriteUShort(_entity.Id);
    foreach (var property in _properties)
    {
      if (property.IsDirty)
      {
        buffer.WriteBool(true);
        property.Write(buffer);
        property.Clear();
        continue;
      }

      buffer.WriteBool(false);
    }
  }

  public void Read(RagonBuffer buffer)
  {
    foreach (var property in _properties)
    {
      if (buffer.ReadBool())
        property.Read(buffer);
    }
  }

  public void Snapshot(RagonBuffer buffer)
  {
    foreach (var property in _properties)
    {
      var hasPayloadOrFixed = property.IsFixed || property is { IsFixed: false, Size: > 0 };
      if (hasPayloadOrFixed)
      {
        buffer.WriteBool(true);
        property.Write(buffer);
        continue;
      }
      
      buffer.WriteBool(false);
    }
  }
}