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

namespace Ragon.Client
{
  [Serializable]
  public class RagonPlayer
  {
    public string Id { get; private set; }
    public string Name { get; set; }
    public ushort PeerId { get; set; }
    public bool IsRoomOwner { get; set; }
    public bool IsLocal { get; set; }
    public RagonUserData UserData { get; private set; }
    
    public RagonPlayer(ushort peerId, string playerId, string name, bool isRoomOwner, bool isLocal)
    {
      PeerId = peerId;
      IsRoomOwner = isRoomOwner;
      IsLocal = isLocal;
      Name = name;
      Id = playerId;
      UserData = new RagonUserData();
    }
  }
}