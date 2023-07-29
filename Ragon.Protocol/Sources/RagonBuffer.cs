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

/*
 *  Copyright (c) 2018 Stanislav Denisov, Maxim Munnig
 *
 *  Permission is hereby granted, free of charge, to any person obtaining a copy
 *  of this software and associated documentation files (the "Software"), to deal
 *  in the Software without restriction, including without limitation the rights
 *  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 *  copies of the Software, and to permit persons to whom the Software is
 *  furnished to do so, subject to the following conditions:
 *
 *  The above copyright notice and this permission notice shall be included in all
 *  copies or substantial portions of the Software.
 *
 *  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 *  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 *  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 *  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 *  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 *  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 *  SOFTWARE.
 */

/*
 *  Copyright (c) 2018 Alexander Shoulson
 *
 *  This software is provided 'as-is', without any express or implied
 *  warranty. In no event will the authors be held liable for any damages
 *  arising from the use of this software.
 *  Permission is granted to anyone to use this software for any purpose,
 *  including commercial applications, and to alter it and redistribute it
 *  freely, subject to the following restrictions:
 *
 *  1. The origin of this software must not be misrepresented; you must not
 *     claim that you wrote the original software. If you use this software
 *     in a product, an acknowledgment in the product documentation would be
 *     appreciated but is not required.
 *  2. Altered source versions must be plainly marked as such, and must not be
 *     misrepresented as being the original software.
 *  3. This notice may not be removed or altered from any source distribution.
 */


using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Ragon.Protocol
{
  public class RagonBuffer
  {
    private int _read;
    private int _write;
    private uint[] _buckets;
    private readonly UTF8Encoding _utf8Encoding = new UTF8Encoding(false, true);

    public int ReadOffset => _read;
    public int WriteOffset => _write;
    public int Length => ((_write - 1) >> 3) + 1;
    public int Capacity => _write - _read;

    public RagonBuffer(int capacity = 128)
    {
      _buckets = new uint[capacity];
      _read = 0;
      _write = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteBool(bool value)
    {
      Write(value ? 1u : 0u, 1);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ReadBool()
    {
      return Read(1) == 1u;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteByte(byte value)
    {
      Write(value, 8);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte ReadByte()
    {
      return (byte)Read(8);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteOperation(RagonOperation operation)
    {
      Write((byte)operation, 8);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public RagonOperation ReadOperation()
    {
      return (RagonOperation)Read(8);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteFloat(float value, float min, float max, float precision)
    {
      var requiredBits = DeBruijn.Log2((uint)((max - min) * (1.0f / precision) + 0.5f)) + 1;
      var mask = (uint)((1L << requiredBits) - 1);
      var compressedValue = (uint)((value - min) * (1f / precision) + 0.5f) & mask;

      Write(compressedValue, requiredBits);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float ReadFloat(float min, float max, float precision)
    {
      var requiredBits = DeBruijn.Log2((uint)((max - min) * (1.0f / precision) + 0.5f)) + 1;
      var compressedValue = Read(requiredBits);

      float adjusted = compressedValue * precision + min;

      if (adjusted < min)
        adjusted = min;
      else if (adjusted > max)
        adjusted = max;

      return adjusted;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteInt(int value, int min, int max)
    {
      var maxValue = Math.Max(Math.Abs(min), Math.Abs(max));
      var requiredBits = Bits.Compute(maxValue);
      uint compressedValue = (uint)((value << 1) ^ (value >> 31));

      Write(compressedValue, requiredBits);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int ReadInt(int min, int max)
    {
      var maxValue = Math.Max(Math.Abs(min), Math.Abs(max));
      var requiredBits = Bits.Compute(maxValue);
      var compressedValue = Read(requiredBits);
      var value = (int)((compressedValue >> 1) ^ (-(int)(compressedValue & 1)));

      return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteUShort(ushort value)
    {
      Write(value, 16);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort ReadUShort()
    {
      return (ushort)Read(16);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteString(string str)
    {
      var data = _utf8Encoding.GetBytes(str);
      var len = (uint)data.Length;
      Write(len, 16);
      WriteBytes(data);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ReadString()
    {
      var len = (int)Read(16);
      var data = ReadBytes(len);
      var str = _utf8Encoding.GetString(data);
      return str;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint Read(int numBits, int offset)
    {
      var currentBucketIndex = offset >> 5;
      var used = offset & 0x0000001F;

      var chunkMask = ((1UL << numBits) - 1) << used;
      var scratch = (ulong)_buckets[currentBucketIndex];

      if (currentBucketIndex + 1 < _buckets.Length)
        scratch |= (ulong)_buckets[currentBucketIndex + 1] << 32;

      var result = (scratch & chunkMask) >> used;

      return (uint)result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(uint value, int numBits, int offset)
    {
      Debug.Assert(!(numBits < 0));
      Debug.Assert(!(numBits > 32));

      var index = offset >> 5;
      var used = offset & 0x0000001F;

      var valueMask = (1UL << numBits) - 1;
      var prepared = (value & valueMask) << used;
      var scratch = _buckets[index] | (ulong)_buckets[index + 1] << 32;
      var result = scratch | prepared;

      _buckets[index] = (uint)result;
      _buckets[index + 1] = (uint)(result >> 32);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(uint value, int numBits = 16)
    {
      Debug.Assert(!(numBits < 0));
      Debug.Assert(!(numBits > 32));

      var currentBucketIndex = _write >> 5;
      var used = _write & 0x0000001F;
      var mask = (1UL << used) - 1;
      var scratch = _buckets[currentBucketIndex] & mask;
      var result = scratch | ((ulong)value << used);

      if (currentBucketIndex + 1 >= _buckets.Length)
        Resize(1);

      _buckets[currentBucketIndex] = (uint)result;
      _buckets[currentBucketIndex + 1] = (uint)(result >> 32);

      _write += numBits;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint Read(int numBits = 16)
    {
      var currentBucketIndex = _read >> 5;
      var used = _read & 0x0000001F;

      var chunkMask = ((1UL << numBits) - 1) << used;
      var scratch = (ulong)_buckets[currentBucketIndex];

      if (currentBucketIndex + 1 < _buckets.Length)
        scratch |= (ulong)_buckets[currentBucketIndex + 1] << 32;

      var result = (scratch & chunkMask) >> used;

      _read += numBits;

      return (uint)result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [Obsolete("Do not use this method, will be removed")]
    public void WriteBytes(byte[] data)
    {
      var len = data.Length;
      for (int i = 0; i < len; i++)
        Write(data[i], 8);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [Obsolete("Do not use this method, will be removed")]
    public byte[] ReadBytes(int lenght)
    {
      var data = new byte[lenght];
      for (int i = 0; i < lenght; i++)
        data[i] = (byte)Read(8);

      return data;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ReadArray(uint[] data, int size)
    {
      var used = _read & 0x0000001F;
      var index = _read >> 5;
      var limit = (size + 32 - 1) / 32;
      var capacity = size;

      for (int i = 0; i < limit; i++)
      {
        var dataSize = capacity > 32 ? 32 : capacity;
        var mask = (1UL << dataSize) - 1;
        var bucketRaw = (ulong)_buckets[index];
        if (index + 1 < _buckets.Length)
          bucketRaw |= (ulong)_buckets[index + 1] << 32;

        var bucket = bucketRaw >> used;
        var result = bucket & mask;

        data[i] = (uint)result;
        if (i + 1 < data.Length)
          data[i + 1] = (uint)(result >> 32);

        index += 1;
        capacity -= dataSize;
      }

      _read += size;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteArray(uint[] data, int size)
    {
      var used = _write & 0x0000001F;
      var index = _write >> 5;
      var limit = (size + 32 - 1) / 32;

      if (index + limit >= _buckets.Length)
        Resize(size);

      for (var i = 0; i < limit; i += 1)
      {
        var prepared = (ulong)data[i] << used;
        var mask = (1UL << used) - 1;
        var scratch = _buckets[index] & mask;
        var result = scratch | prepared;

        _buckets[index] = (uint)result;
        _buckets[index + 1] = (uint)(result >> 32);

        index += 1;
      }

      _write += size;
    }

    public void Clear()
    {
      _read = 0;
      _write = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void FromBuffer(RagonBuffer buffer, int size)
    {
      WriteArray(buffer._buckets, size);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ToBuffer(RagonBuffer buffer, int size)
    {
      ReadArray(buffer._buckets, size);
    }

    public void FromArray(byte[] data)
    {
      var length = data.Length;
      var bucketsCount = length / 4 + 1;

      if (_buckets.Length < bucketsCount)
        _buckets = new uint[bucketsCount];

      for (var i = 0; i < bucketsCount; i++)
      {
        var dataIdx = i * 4;
        var bucket = 0u;

        if (dataIdx < length)
          bucket = data[dataIdx];

        if (dataIdx + 1 < length)
          bucket |= (uint)data[dataIdx + 1] << 8;

        if (dataIdx + 2 < length)
          bucket |= (uint)data[dataIdx + 2] << 16;

        if (dataIdx + 3 < length)
          bucket |= (uint)data[dataIdx + 3] << 24;

        _buckets[i] = bucket;
      }

      int positionInByte = Bits.FindBitPosition(data[length - 1]);

      _write = ((length - 1) * 8) + positionInByte;
      _read = 0;
    }

    public byte[] ToArray()
    {
      var data = new byte[Length];
      int bucketsCount = (_write >> 5) + 1;
      int length = data.Length;

      for (int i = 0; i < bucketsCount; i++)
      {
        int dataIdx = i * 4;
        uint bucket = _buckets[i];

        if (dataIdx < length)
          data[dataIdx] = (byte)(bucket);

        if (dataIdx + 1 < length)
          data[dataIdx + 1] = (byte)(bucket >> 8);

        if (dataIdx + 2 < length)
          data[dataIdx + 2] = (byte)(bucket >> 16);

        if (dataIdx + 3 < length)
          data[dataIdx + 3] = (byte)(bucket >> 24);
      }

      return data;
    }

    private void Resize(int capacity)
    {
      var buckets = new uint[_buckets.Length * 2 + capacity];
      Array.Copy(_buckets, buckets, _buckets.Length);
      _buckets = buckets;
    }
  }
}