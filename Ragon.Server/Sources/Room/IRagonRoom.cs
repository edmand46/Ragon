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

using Ragon.Server.Entity;
using Ragon.Server.IO;

namespace Ragon.Server.Room;

public interface IRagonRoom
{
  RagonRoomPlayer GetPlayerByConnection(INetworkConnection connection);
  RagonRoomPlayer GetPlayerById(string id);
  IRagonEntity GetEntityById(ushort id);
  IRagonEntity[] GetEntitiesOfPlayer(RagonRoomPlayer id);
}