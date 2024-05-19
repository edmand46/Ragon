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

namespace Ragon.Client
{
  [Serializable]
  public class RagonFloat : RagonProperty
  {
    public float Value
    {
      get => _value;
      set
      {
        _value = value;

        if (_value < _min)
          _value = _min;
        else if (_value > _max)
          _value = _max;

        MarkAsChanged();
      }
    }

    private float _value;
    private float _min;
    private float _max;
    private int _requiredBits;
    private float _precision;
    private uint _mask;

    public RagonFloat(
      float initialValue,
      bool invokeLocal = true,
      int priority = 0
    ) : base(priority, invokeLocal)
    {
      _value = initialValue;
      _min = -1024.0f;
      _max = 1024.0f;
      _precision = 0.01f;

      _requiredBits = DeBruijn.Log2((uint)((_max - _min) * (1.0f / _precision) + 0.5f)) + 1;
      _mask = (uint)((1L << _requiredBits) - 1);

      SetFixedSize(_requiredBits);
    }

    public RagonFloat(
      float initialValue,
      float min = -1024.0f,
      float max = 1024.0f,
      float precision = 0.01f,
      bool invokeLocal = true,
      int priority = 0
    ) : base(priority, invokeLocal)
    {
      _value = initialValue;
      _min = min;
      _max = max;
      _precision = precision;

      _requiredBits = DeBruijn.Log2((uint)((_max - _min) * (1.0f / _precision) + 0.5f)) + 1;
      _mask = (uint)((1L << _requiredBits) - 1);

      SetFixedSize(_requiredBits);
    }

    public override void Serialize(RagonBuffer buffer)
    {
      var compressedValue = (uint)((_value - _min) * (1f / _precision) + 0.5f) & _mask;
      buffer.Write(compressedValue, _requiredBits);
    }

    public override void Deserialize(RagonBuffer buffer)
    {
      var compressedValue = buffer.Read(_requiredBits);
      _value = compressedValue * _precision + _min;

      if (_value < _min)
        _value = _min;
      else if (_value > _max)
        _value = _max;
      
      InvokeChanged();
    }
  }
}