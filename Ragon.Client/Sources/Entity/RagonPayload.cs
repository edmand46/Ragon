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

namespace Ragon.Client;

public class RagonPayload
{
  private readonly uint[] _data = new uint[128];
  private readonly int _size = 0;

  public RagonPayload(int capacity)
  {
    _size = capacity;
  }
  public int Size => _size;

  public void Read(RagonBuffer buffer)
  {
    buffer.ReadArray(_data, _size);
  }

  public void Write(RagonBuffer buffer)
  {
    buffer.WriteArray(_data, _size);
  }

  public override string ToString()
  {
    return $"Payload Size: {_size}";
  }
}