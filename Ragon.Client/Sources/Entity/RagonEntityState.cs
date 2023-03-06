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

namespace Ragon.Client;

public sealed class RagonEntityState
{
  private List<RagonProperty> _properties;
  private RagonEntity _entity;
  
  public RagonEntityState(RagonEntity entity)
  {
    _entity = entity;
    _properties = new List<RagonProperty>(6);
  }
  
  public void AddProperty(RagonProperty property)
  {
    _properties.Add(property);
    
    property.AssignEntity(_entity);
  }
  
  internal void WriteInfo(RagonBuffer buffer)
  {
    buffer.WriteUShort((ushort)_properties.Count);
    foreach (var property in _properties)
    {
      buffer.WriteBool(property.IsFixed);
      buffer.WriteUShort((ushort)property.Size);
    }
  }
  
  internal void ReadState(RagonBuffer buffer)
  {
    foreach (var property in _properties)
    {
      var changed = buffer.ReadBool();
      if (changed)
        property.Deserialize(buffer);
    }
  }

  internal void WriteState(RagonBuffer buffer)
  {
    foreach (var prop in _properties)
    {
      if (prop.IsDirty)
      {
        buffer.WriteBool(true);
        prop.Write(buffer);
        prop.Flush();
      }
      else
      {
        prop.AddTick();
        buffer.WriteBool(false);
      }
    }
  }
}