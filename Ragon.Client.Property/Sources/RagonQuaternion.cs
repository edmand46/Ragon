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

using System.Numerics;
using Ragon.Client.Compressor;
using Ragon.Protocol;

namespace Ragon.Client.Property;

public class RagonQuaternion : RagonProperty
{
  private Quaternion _value;

  public Quaternion Value
  {
    get => _value;
    set
    {
      _value = value;

      MarkAsChanged();
    }
  }

  private readonly FloatCompressor _compressor;

  public RagonQuaternion(bool invokeLocal = false, int priority = 0) : base(priority, invokeLocal)
  {
    _compressor = new FloatCompressor(-1.0f, 1f, 0.01f);

    SetFixedSize(_compressor.RequiredBits * 4);
  }

  public override void Serialize(RagonBuffer buffer)
  {
    var compressedX = _compressor.Compress(_value.X);
    var compressedY = _compressor.Compress(_value.Y);
    var compressedZ = _compressor.Compress(_value.Z);
    var compressedW = _compressor.Compress(_value.W);

    buffer.Write(compressedX, _compressor.RequiredBits);
    buffer.Write(compressedY, _compressor.RequiredBits);
    buffer.Write(compressedZ, _compressor.RequiredBits);
    buffer.Write(compressedW, _compressor.RequiredBits);
  }

  public override void Deserialize(RagonBuffer buffer)
  {
    var compressedX = buffer.Read(_compressor.RequiredBits);
    var compressedY = buffer.Read(_compressor.RequiredBits);
    var compressedZ = buffer.Read(_compressor.RequiredBits);
    var compressedW = buffer.Read(_compressor.RequiredBits);

    var x = _compressor.Decompress(compressedX);
    var y = _compressor.Decompress(compressedY);
    var z = _compressor.Decompress(compressedZ);
    var w = _compressor.Decompress(compressedW);

    _value = new Quaternion(x, y, z, w);

    InvokeChanged();
  }
}