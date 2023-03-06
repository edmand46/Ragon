﻿/*
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

public class IntCompressor
{
  public int Min { get; private set; }
  public int Max { get; private set; }
  public int RequiredBits { get; private set; }

  public IntCompressor(int min = -1000, int max = 1000)
  {
    Min = min;
    Max = max;
    RequiredBits = Bits.Compute(Max);
  }

  public uint Compress(int value)
  {
    return (uint)((value << 1) ^ (value >> 31));;
  }

  public int Decompress(uint value)
  {
    return (int)((value >> 1) ^ -(int)(value & 1));
  }
}