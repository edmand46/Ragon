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

using System.Net.WebSockets;
using Ragon.Server.IO;

namespace Ragon.Server.WebSocketServer;

public class WebSocketReliableChannel : INetworkChannel
{
    private Queue<byte[]> _queue;
    private WebSocket _socket;

    public WebSocketReliableChannel(WebSocket webSocket)
    {
        _socket = webSocket;
        _queue = new Queue<byte[]>(512);
    }

    public void Send(byte[] data)
    {
        _queue.Enqueue(data);
    }

    public async Task Flush()
    {
        while (_queue.TryDequeue(out var sendData) && _socket.State == WebSocketState.Open)
            await _socket.SendAsync(sendData, WebSocketMessageType.Binary, WebSocketMessageFlags.EndOfMessage, CancellationToken.None);
    }
}