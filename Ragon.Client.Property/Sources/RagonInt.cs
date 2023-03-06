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

namespace Ragon.Client.Simulation
{
  [Serializable]
  public class RagonInt : RagonProperty
  {
    public int Value
    {
      get => _value;
      set
      {
        _value = value;
        MarkAsChanged();
      }
    }

    private int _min;
    private int _max;
    private int _requiredBits;
    private int _value;

    public RagonInt(
      int initialValue,
      bool invokeLocal = true,
      int priority = 0
    ) : base(priority, invokeLocal)
    {
      _value = initialValue;
      _min = -1000;
      _max = 1000;

      _max = Math.Max(Math.Abs(_min), Math.Abs(_max));
      _requiredBits = Bits.Compute(_max);

      SetFixedSize(_requiredBits);
    }

    public RagonInt(
      int initialValue,
      int min = -1000,
      int max = 1000,
      bool invokeLocal = true,
      int priority = 0
    ) : base(priority, invokeLocal)
    {
      _value = initialValue;
      _min = min;
      _max = max;

      _requiredBits = Bits.Compute(_max);

      SetFixedSize(_requiredBits);
    }

    public override void Serialize(RagonBuffer buffer)
    {
      uint compressedValue = (uint)((_value << 1) ^ (_value >> 31));
      buffer.Write(compressedValue, _requiredBits);
    }

    public override void Deserialize(RagonBuffer buffer)
    {
      var compressedValue = buffer.Read(_requiredBits);
      _value = (int)((compressedValue >> 1) ^ -(int)(compressedValue & 1));

      InvokeChanged();
    }
  }
}