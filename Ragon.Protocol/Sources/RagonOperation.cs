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


namespace Ragon.Protocol
{
  public enum RagonOperation : byte
  {
    AUTHORIZE = 1,
    AUTHORIZED_SUCCESS = 2,
    AUTHORIZED_FAILED = 3,
    JOIN_OR_CREATE_ROOM = 4,
    CREATE_ROOM = 5,
    JOIN_ROOM = 6,
    LEAVE_ROOM = 7,
    OWNERSHIP_ROOM_CHANGED = 9,
    JOIN_SUCCESS = 10,
    JOIN_FAILED = 11,
    PLAYER_JOINED = 14,
    PLAYER_LEAVED = 15,
    REPLICATE_ROOM_EVENT = 22,
    TRANSFER_ROOM_OWNERSHIP = 23,
    TIMESTAMP_SYNCHRONIZATION = 25,
    ROOM_LIST_UPDATED = 26,
    PLAYER_DATA_UPDATED = 27,
    ROOM_DATA_UPDATED = 28,
  }
}