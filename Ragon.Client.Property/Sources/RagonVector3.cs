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

using System;
using System.Numerics;
using Ragon.Client.Compressor;
using Ragon.Protocol;

namespace Ragon.Client.Property
{
  [Serializable]
  public enum RagonAxis
  {
    XYZ,
    XY,
    YZ,
    XZ,
    X,
    Y,
    Z
  }

  [Serializable]
  public class RagonVector3 : RagonProperty
  {
    private Vector3 _value;

    public Vector3 Value
    {
      get => _value;
      set
      {
        _value = value;
        
        MarkAsChanged();
      }
    }

    private RagonAxis _axis;
    private FloatCompressor _compressorX;
    private FloatCompressor _compressorY;
    private FloatCompressor _compressorZ;


    public RagonVector3(
      RagonAxis axis = RagonAxis.XYZ,
      bool invokeLocal = true,
      int priority = 0
    ) : base(priority, invokeLocal)
    {
      _axis = axis;

      var defaultCompressor = new FloatCompressor(-1024.0f, 1024f, 0.01f);

      _compressorX = defaultCompressor;
      _compressorY = defaultCompressor;
      _compressorZ = defaultCompressor;

      switch (_axis)
      {
        case RagonAxis.XYZ:
          SetFixedSize(_compressorX.RequiredBits + _compressorY.RequiredBits + _compressorZ.RequiredBits);
          break;
        case RagonAxis.XY:
          SetFixedSize(_compressorX.RequiredBits + _compressorY.RequiredBits);
          break;
        case RagonAxis.XZ:
          SetFixedSize(_compressorX.RequiredBits + _compressorZ.RequiredBits);
          break;
        case RagonAxis.YZ:
          SetFixedSize(_compressorY.RequiredBits + _compressorZ.RequiredBits);
          break;
        case RagonAxis.X:
          SetFixedSize(_compressorX.RequiredBits);
          break;
        case RagonAxis.Y:
          SetFixedSize(_compressorY.RequiredBits);
          break;
        case RagonAxis.Z:
          SetFixedSize(_compressorZ.RequiredBits);
          break;
      }
    }

    public RagonVector3(
      Vector3 initialValue,
      RagonAxis axis = RagonAxis.XYZ,
      float min = -1024.0f,
      float max = 1024.0f,
      float precision = 0.1f,
      bool invokeLocal = true,
      int priority = 0
    ) : base(priority, invokeLocal)
    {
      _value = initialValue;
      _axis = axis;
      
      var defaultCompressor = new FloatCompressor(min, max, precision);

      _compressorX = defaultCompressor;
      _compressorY = defaultCompressor;
      _compressorZ = defaultCompressor;

      switch (_axis)
      {
        case RagonAxis.XYZ:
          SetFixedSize(_compressorX.RequiredBits + _compressorY.RequiredBits + _compressorZ.RequiredBits);
          break;
        case RagonAxis.XY:
          SetFixedSize(_compressorX.RequiredBits + _compressorY.RequiredBits);
          break;
        case RagonAxis.XZ:
          SetFixedSize(_compressorX.RequiredBits + _compressorZ.RequiredBits);
          break;
        case RagonAxis.YZ:
          SetFixedSize(_compressorY.RequiredBits + _compressorZ.RequiredBits);
          break;
        case RagonAxis.X:
          SetFixedSize(_compressorX.RequiredBits);
          break;
        case RagonAxis.Y:
          SetFixedSize(_compressorY.RequiredBits);
          break;
        case RagonAxis.Z:
          SetFixedSize(_compressorZ.RequiredBits);
          break;
      }
    }

    public RagonVector3(
      RagonAxis axis = RagonAxis.XYZ,
      FloatCompressor compressorX = null,
      FloatCompressor compressorY = null,
      FloatCompressor compressorZ = null,
      bool invokeLocal = true,
      int priority = 0
    ) : base(priority, invokeLocal)
    {
      _axis = axis;

      var defaultCompressor = new FloatCompressor(-1024.0f, 1024f, 0.01f);

      _compressorX = defaultCompressor;
      _compressorY = defaultCompressor;
      _compressorZ = defaultCompressor;

      if (compressorX != null)
        _compressorX = compressorX;

      if (compressorY != null)
        _compressorY = compressorY;

      if (compressorZ != null)
        _compressorZ = compressorZ;

      switch (_axis)
      {
        case RagonAxis.XYZ:
          SetFixedSize(_compressorX.RequiredBits + _compressorY.RequiredBits + _compressorZ.RequiredBits);
          break;
        case RagonAxis.XY:
          SetFixedSize(_compressorX.RequiredBits + _compressorY.RequiredBits);
          break;
        case RagonAxis.XZ:
          SetFixedSize(_compressorX.RequiredBits + _compressorZ.RequiredBits);
          break;
        case RagonAxis.YZ:
          SetFixedSize(_compressorY.RequiredBits + _compressorZ.RequiredBits);
          break;
        case RagonAxis.X:
          SetFixedSize(_compressorX.RequiredBits);
          break;
        case RagonAxis.Y:
          SetFixedSize(_compressorY.RequiredBits);
          break;
        case RagonAxis.Z:
          SetFixedSize(_compressorZ.RequiredBits);
          break;
      }
    }

    public override void Serialize(RagonBuffer buffer)
    {
      switch (_axis)
      {
        case RagonAxis.XYZ:
        {
          var compressedX = _compressorX.Compress(_value.X);
          var compressedY = _compressorY.Compress(_value.Y);
          var compressedZ = _compressorZ.Compress(_value.Z);

          buffer.Write(compressedX, _compressorX.RequiredBits);
          buffer.Write(compressedY, _compressorY.RequiredBits);
          buffer.Write(compressedZ, _compressorZ.RequiredBits);
        }
          break;
        case RagonAxis.XY:
        {
          var compressedX = _compressorX.Compress(_value.X);
          var compressedY = _compressorY.Compress(_value.Y);
          
          buffer.Write(compressedX, _compressorX.RequiredBits);
          buffer.Write(compressedY, _compressorY.RequiredBits);
        }
          break;
        case RagonAxis.XZ:
        {
          var compressedX = _compressorX.Compress(_value.X);
          var compressedZ = _compressorZ.Compress(_value.Z);

          buffer.Write(compressedX, _compressorX.RequiredBits);
          buffer.Write(compressedZ, _compressorZ.RequiredBits);
          break;
        }

        case RagonAxis.YZ:
        {
          var compressedY = _compressorY.Compress(_value.Y);
          var compressedZ = _compressorZ.Compress(_value.Z);

          buffer.Write(compressedY, _compressorY.RequiredBits);
          buffer.Write(compressedZ, _compressorZ.RequiredBits);
          break;
        }
        case RagonAxis.X:
        {
          var compressedX = _compressorX.Compress(_value.X);

          buffer.Write(compressedX, _compressorX.RequiredBits);
          break;
        }
        case RagonAxis.Y:
        {
          var compressedY = _compressorY.Compress(_value.Y);
          
          buffer.Write(compressedY, _compressorY.RequiredBits);
          break;
        }
        case RagonAxis.Z:
        {
          var compressedZ = _compressorZ.Compress(_value.Z);
          
          buffer.Write(compressedZ, _compressorZ.RequiredBits);
          break;
        }
      }
    }

    public override void Deserialize(RagonBuffer buffer)
    {
      switch (_axis)
      {
        case RagonAxis.XYZ:
        {
          var compressedX = buffer.Read(_compressorX.RequiredBits);
          var compressedY = buffer.Read(_compressorY.RequiredBits);
          var compressedZ = buffer.Read(_compressorZ.RequiredBits);

          _value.X = _compressorX.Decompress(compressedX);
          _value.Y = _compressorY.Decompress(compressedY);
          _value.Z = _compressorZ.Decompress(compressedZ);
          break;
        }
        case RagonAxis.XY:
        {
          var compressedX = buffer.Read(_compressorX.RequiredBits);
          var compressedY = buffer.Read(_compressorY.RequiredBits);

          _value.X = _compressorX.Decompress(compressedX);
          _value.Z = _compressorY.Decompress(compressedY);
          break;
        }
        case RagonAxis.XZ:
        {
          var compressedX = buffer.Read(_compressorX.RequiredBits);
          var compressedZ = buffer.Read(_compressorZ.RequiredBits);

          _value.X = _compressorX.Decompress(compressedX);
          _value.Z = _compressorZ.Decompress(compressedZ);
          break;
        }
        case RagonAxis.YZ:
        {
          var compressedY = buffer.Read(_compressorY.RequiredBits);
          var compressedZ = buffer.Read(_compressorZ.RequiredBits);

          _value.Y = _compressorY.Decompress(compressedY);
          _value.Z = _compressorZ.Decompress(compressedZ);
          break;
        }
        case RagonAxis.X:
        {
          var compressedX = buffer.Read(_compressorX.RequiredBits);

          _value.X = _compressorX.Decompress(compressedX);
          break;
        }
        case RagonAxis.Y:
        {
          var compressedY = buffer.Read(_compressorY.RequiredBits);

          _value.Y = _compressorY.Decompress(compressedY);
          break;
        }
        case RagonAxis.Z:
        {
          var compressedZ = buffer.Read(_compressorZ.RequiredBits);

          _value.Z = _compressorZ.Decompress(compressedZ);
          break;
        }
      }

      InvokeChanged();
    }
  }
}