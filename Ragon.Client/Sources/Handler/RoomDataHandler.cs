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


using Ragon.Protocol;

namespace Ragon.Client;

internal class RoomDataHandler: IHandler
{
  private readonly RagonListenerList _listeners;
  private readonly RagonPlayerCache _playerCache;
    
  public RoomDataHandler(
    RagonPlayerCache playerCache,
    RagonListenerList listeners)
  {
    _playerCache = playerCache;
    _listeners = listeners;
  }

  public void Handle(RagonStream reader)
  {
    var rawData = reader.ReadBinary(reader.Lenght);
    var peerId = (ushort)(rawData[1] + (rawData[2] << 8));
    
    RagonPlayer player = null;
    if (peerId != 10000)
    {
      player = _playerCache.GetPlayerByPeer(peerId);
      if (player == null)
      {
        RagonLog.Error($"Player with peerId:{peerId} not found");

        _playerCache.Dump();
        return;
      }
    }

    var headerSize = 3;
    var payload = new byte[rawData.Length - headerSize];
    
    Array.Copy(rawData, headerSize, payload, 0, payload.Length);
    
    _listeners.OnData(player, payload);
  }
}