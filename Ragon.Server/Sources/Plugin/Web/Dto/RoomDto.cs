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

using Ragon.Server.Room;

namespace Ragon.Server.Plugin.Web;

[Serializable]
public class RoomDto
{
  public string Id { get; set;}
  public int PlayerMin { get; set; }
  public int PlayerMax { get; set; }
  public int PlayerCount { get; set; }
  public PlayerDto[] Players { get; set; }

  public RoomDto(RagonRoom room)
  {
    Id = room.Id;
    PlayerMin = room.PlayerMin;
    PlayerMax = room.PlayerMax;
    PlayerCount = room.PlayerCount;

    Players = room.PlayerList.Select(p => new PlayerDto(p)).ToArray();
  }
}