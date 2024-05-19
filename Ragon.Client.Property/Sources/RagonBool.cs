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


using Ragon.Protocol;

namespace Ragon.Client.Property
{
  [Serializable]
  public class RagonBool : RagonProperty
  {
    public bool Value
    {
      get => _value;
      set
      {
        _value = value;
        
        MarkAsChanged();
      }
    }

    private bool _value;

    public RagonBool(
      bool initialValue,
      bool invokeLocal = true,
      int priority = 0
    ) : base(priority, invokeLocal)
    {
      _value = initialValue;
      SetFixedSize(1);
    }

    public override void Serialize(RagonBuffer buffer)
    {
      buffer.WriteBool(_value);
    }

    public override void Deserialize(RagonBuffer buffer)
    {
      _value = buffer.ReadBool();

      InvokeChanged();
    }
  }
}