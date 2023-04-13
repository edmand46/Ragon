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
    AUTHORIZE,
    AUTHORIZED_SUCCESS,
    AUTHORIZED_FAILED,
    JOIN_OR_CREATE_ROOM,
    CREATE_ROOM,
    JOIN_ROOM,
    LEAVE_ROOM,
    OWNERSHIP_CHANGED,
    JOIN_SUCCESS,
    JOIN_FAILED,
    LOAD_SCENE,
    SCENE_LOADED,
    PLAYER_JOINED,
    PLAYER_LEAVED,
    CREATE_ENTITY,
    REMOVE_ENTITY,
    SNAPSHOT,
    REPLICATE_ENTITY_STATE,
    REPLICATE_ENTITY_EVENT,
  }
}