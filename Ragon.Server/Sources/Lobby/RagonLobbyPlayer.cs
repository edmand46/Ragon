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

using Ragon.Server.IO;

namespace Ragon.Server.Lobby;

public enum ConnectionStatus
{
  Unauthorized,
  InProcess,
  Authorized,
}

public class RagonLobbyPlayer
{
  public INetworkConnection Connection { get; }
  public string Id { get; private set; }
  public string Name { get; private set; }
  public string Payload { get; private set; }
  
  public RagonLobbyPlayer(INetworkConnection connection, string id, string name, string payload)
  {
    Id = id;
    Name = name;
    Connection = connection;
    Payload = payload;
  }
}