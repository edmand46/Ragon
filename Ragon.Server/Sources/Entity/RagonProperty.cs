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

namespace Ragon.Server.Entity;

public class RagonProperty : RagonPayload
{
  public int Size { get; set; }
  public bool IsDirty { get; private set; }
  public bool IsFixed { get; private set; }
  public bool HasData { get; private set; }

  private uint[] _data;

  public RagonProperty(int size, bool isFixed, int limit)
  {
    Size = size;
    IsFixed = isFixed;
    IsDirty = false;
    
    _data = new uint[limit / 4 + 1]; 
  }

  public void Read(RagonBuffer buffer)
  {
    if (IsFixed)
    {
      buffer.ReadArray(_data, Size);
    }
    else
    {
      Size = (int) buffer.Read();
      buffer.ReadArray(_data, Size);
    }
    
    HasData = true;
    IsDirty = true;
  }

  public void Write(RagonBuffer buffer)
  {
    if (IsFixed)
    {
      buffer.WriteArray(_data, Size);
      return;
    }
    
    buffer.Write((ushort) Size);
    buffer.WriteArray(_data, Size);
  }

  public void Clear()
  {
    IsDirty = false;
  }
  
  public void Dump()
  {
    Console.WriteLine( $"[{Size.ToString("00")}] {string.Join("", _data.Take(8).Reverse().Select(b => Convert.ToString(b, 2).PadLeft(32, '0')))}");
  }
}