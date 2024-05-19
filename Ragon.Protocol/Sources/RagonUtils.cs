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


using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ragon.Protocol
{
  [StructLayout(LayoutKind.Explicit)]
  public struct DoubleToUInt
  {
    [FieldOffset(0)]
    public double Double;
    
    [FieldOffset(0)]
    public uint Int0;
    
    [FieldOffset(4)]
    public uint Int1;
  }
  
  public static class RagonTime {
    public static double CurrentTimestamp()
    {
      var currentTime = System.DateTime.UtcNow.ToUniversalTime().Subtract(
        new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc)
      ).TotalMilliseconds;
      
      return currentTime; 
    }
  }
  
  public static class DeBruijn
  {
    private static readonly int[] _lookup = new int[32]
    {
      0, 9, 1, 10, 13, 21, 2, 29, 11, 14, 16, 18, 22, 25, 3, 30,
      8, 12, 20, 28, 15, 17, 24, 7, 19, 27, 23, 6, 26, 5, 4, 31
    };

    public static int Log2(uint value)
    {
      value |= value >> 1;
      value |= value >> 2;
      value |= value >> 4;
      value |= value >> 8;
      value |= value >> 16;

      return _lookup[(value * 0x07C4ACDDU) >> 27];
    }
  }
  
  public static class Bits
  {
    static int[] _lookup = new int[256];

    static Bits()
    {
      _lookup[0] = 0;
      for (int i = 0; i < 256; i++)
        _lookup[i] = (i & 1) + _lookup[i / 2];
    }

    [MethodImpl(256)]
    public static int Compute(int value)
    {
      var count = 0;
      do
      {
        value >>= 8;
        count += 8;
      } while (value > 0);

      return count;
    }

    [MethodImpl(256)]
    public static int FindBitPosition(byte data)
    {
      int shiftCount = 0;

      while (data > 0)
      {
        data >>= 1;
        shiftCount++;
      }

      return shiftCount;
    }
  }
}