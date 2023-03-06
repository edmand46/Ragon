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

namespace Ragon.Client.Compressor;

public class FloatCompressor
{
  public float Min { get; private set; }
  public float Max { get; private set; }
  public float Precision { get; private set; }
  public int RequiredBits { get; private set; }
  
  private uint _mask;

  public FloatCompressor(float min = -1024.0f, float max = 1024.0f, float precision = 0.01f)
  {
    Min = min;
    Max = max;
    Precision = precision;
    RequiredBits = DeBruijn.Log2((uint)((Max - Min) * (1f / Precision) + 0.5f)) + 1;
    
    _mask = (uint)((1L << RequiredBits) - 1);
  }

  public uint Compress(float value)
  {
    return (uint)((value - Min) * 1f / Precision + 0.5f) & _mask;
  }

  public float Decompress(uint value)
  {
    var result = value * Precision + Min;

    if (result < Min)
      result = Min;
    else if (result > Max)
      result = Max;

    return result;
  }
}