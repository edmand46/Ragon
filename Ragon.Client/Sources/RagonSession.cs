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
  public class RagonSession
  {
    private readonly RagonClient _client;
    private readonly RagonBuffer _buffer;
    
    public RagonSession(RagonClient client, RagonBuffer buffer)
    {
      _client = client;
      _buffer = buffer;
    }

    public void CreateOrJoin(string map, int minPlayers, int maxPlayers)
    {
      var parameters = new RagonRoomParameters() {Map = map, Min = minPlayers, Max = maxPlayers};
      CreateOrJoin(parameters);
    }
    
    public void CreateOrJoin(RagonRoomParameters parameters)
    {
      _buffer.Clear();
      _buffer.WriteOperation(RagonOperation.JOIN_OR_CREATE_ROOM);

      parameters.Serialize(_buffer);

      var sendData = _buffer.ToArray();
      _client.Reliable.Send(sendData);
    }

    public void Create(string map, int minPlayers, int maxPlayers)
    {
      Create(null, new RagonRoomParameters() {Map = map, Min = minPlayers, Max = maxPlayers});
    }

    public void Create(string roomId, string map, int minPlayers, int maxPlayers)
    {
      Create(roomId, new RagonRoomParameters() {Map = map, Min = minPlayers, Max = maxPlayers});
    }
    
    public  void Create(string roomId, RagonRoomParameters parameters)
    {
      _buffer.Clear();
      _buffer.WriteOperation(RagonOperation.CREATE_ROOM);

      if (roomId != null)
      {
        _buffer.WriteBool(true);
        _buffer.WriteString(roomId);
      }
      else
      {
        _buffer.WriteBool(false);
      }

      parameters.Serialize(_buffer);

      var sendData = _buffer.ToArray();
      _client.Reliable.Send(sendData);
    }
    
    public  void Leave()
    {
      var sendData = new[] {(byte) RagonOperation.LEAVE_ROOM};
      _client.Reliable.Send(sendData);
    }

    public  void Join(string roomId)
    {
      _buffer.Clear();
      _buffer.WriteOperation(RagonOperation.JOIN_ROOM);
      _buffer.WriteString(roomId);

      var sendData = _buffer.ToArray();
      _client.Reliable.Send(sendData);
    }

    public  void AuthorizeWithKey(string key, string playerName, string payload = "")
    {
      _buffer.Clear();
      _buffer.WriteOperation(RagonOperation.AUTHORIZE);
      _buffer.WriteString(key);
      _buffer.WriteString(playerName);
      _buffer.WriteString(payload);

      var sendData = _buffer.ToArray();
      _client.Reliable.Send(sendData);
    }
    
  }
}