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
  public enum RagonOperation: byte
  {
    AUTHORIZE = 1,
    AUTHORIZED_SUCCESS = 2,
    AUTHORIZED_FAILED = 3,
    JOIN_OR_CREATE_ROOM = 4,
    CREATE_ROOM = 5,
    JOIN_ROOM = 6,
    LEAVE_ROOM = 7,
    OWNERSHIP_ENTITY_CHANGED = 8,
    OWNERSHIP_ROOM_CHANGED= 9,
    JOIN_SUCCESS = 10,
    JOIN_FAILED = 11,
    LOAD_SCENE = 12,
    SCENE_LOADED = 13,
    PLAYER_JOINED = 14,
    PLAYER_LEAVED = 15,
    CREATE_ENTITY = 16,
    REMOVE_ENTITY = 17,
    SNAPSHOT = 18,
    REPLICATE_ENTITY_STATE = 19,
    REPLICATE_ENTITY_EVENT = 20,
    REPLICATE_RAW_DATA = 21,
    REPLICATE_ROOM_EVENT = 22,
    TRANSFER_ROOM_OWNERSHIP = 23,
    TRANSFER_ENTITY_OWNERSHIP = 24,
    TIMESTAMP_SYNCHRONIZATION = 25,
  }
}