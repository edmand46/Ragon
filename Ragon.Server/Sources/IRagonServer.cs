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
using Ragon.Server.Handler;
using Ragon.Server.IO;
using Ragon.Server.Lobby;

namespace Ragon.Server;

public interface IRagonServer
{
  BaseOperation ResolveHandler(RagonOperation operation);
  public RagonContext? GetContextByConnectionId(ushort peerId);
  public RagonContext? GetContextById(string playerId);
}