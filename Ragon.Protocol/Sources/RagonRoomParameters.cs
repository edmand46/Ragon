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


namespace Ragon.Protocol
{
  public class RagonRoomParameters: IRagonSerializable
  {
    public string Scene { get; set; }
    public int Min { get; set; } 
    public int Max { get; set; } 
    
    public void Serialize(RagonBuffer buffer)
    {
      buffer.WriteString(Scene);
      buffer.WriteInt(Min, 1, 32);
      buffer.WriteInt(Max, 1, 32);
    }

    public void Deserialize(RagonBuffer buffer)
    {
      Scene = buffer.ReadString();
      Min = buffer.ReadInt(1, 32);
      Max = buffer.ReadInt(1, 32);
    }
  }
}