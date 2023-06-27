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

namespace Ragon.Client
{
  [Serializable]
  public class RagonProperty
  {
    public string Name => _name;
    public RagonEntity Entity => _entity;

    public event Action Changed;
    public bool IsDirty => _dirty && _ticks >= _priority;
    public bool IsFixed => _fixed;
    public int Size => _size;

    private RagonEntity _entity;    
    private bool _dirty;
    private int _size;
    private int _ticks;
    private int _priority;
    private bool _fixed;
    private string _name;

    protected bool InvokeLocal;

    protected RagonProperty(int priority, bool invokeLocal)
    {
      _size = 0;
      _priority = priority;
      _fixed = false;
      
      InvokeLocal = invokeLocal;
    }

    public void SetName(string name)
    {
      _name = name;
    }

    protected void SetFixedSize(int size)
    {
      _size = size;
      _fixed = true;
    }

    protected void InvokeChanged()
    {
      if (!InvokeLocal)
        return;

      Changed?.Invoke();
    }

    protected void MarkAsChanged()
    {
      InvokeChanged();

      if (_dirty || _entity == null)
        return;

      _dirty = true;
      _entity.TrackChangedProperty(this);
    }

    internal void Flush()
    {
      _dirty = false;
      _ticks = 0;
    }

    internal void AddTick()
    {
      _ticks++;
    }

    internal void AssignEntity(RagonEntity ent)
    {
      _entity = ent;

      Changed?.Invoke();
    }

    internal void Write(RagonBuffer buffer)
    {
      if (_fixed)
      {
        Serialize(buffer);
        return;
      }

      var sizeOffset = buffer.WriteOffset;
      buffer.Write(0, 16);
      var propOffset = buffer.WriteOffset;

      Serialize(buffer);

      var propSize = (uint)(buffer.WriteOffset - propOffset);
      buffer.Write(propSize, 16, sizeOffset);
    }

    internal void Read(RagonBuffer buffer)
    {
      Deserialize(buffer);
    }

    public virtual void Serialize(RagonBuffer buffer)
    {
    }

    public virtual void Deserialize(RagonBuffer buffer)
    {
    }
  }
}