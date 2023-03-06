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

namespace Ragon.Server;

public enum LobbyPlayerStatus
{
  Unauthorized,
  Authorized,
}

public class RagonLobbyPlayer
{
  public string Id { get; private set; }
  public string Name { get; set; }
  public byte[] AdditionalData { get; set; }
  public LobbyPlayerStatus Status { get; set; }
  public INetworkConnection Connection { get; private set; }
  
  public RagonLobbyPlayer(INetworkConnection connection)
  {
    Id = Guid.NewGuid().ToString();
    Connection = connection;
    Status = LobbyPlayerStatus.Unauthorized;
    Name = "None";
    AdditionalData = Array.Empty<byte>();
  }
}