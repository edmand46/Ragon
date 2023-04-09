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
using NLog;

namespace Ragon.Server.DotNetWebsockets;

public sealed class WebSocketConnection : INetworkConnection
{
    private Logger _logger = LogManager.GetCurrentClassLogger();
    public ushort Id { get; }
    public INetworkChannel Reliable { get; private set; }
    public INetworkChannel Unreliable { get; private set; }

    public WebSocket Socket { get; private set; }
    private WebSocketReliableChannel[] _channels;

    public WebSocketConnection(WebSocket webSocket, ushort peerId)
    {
        Id = peerId;
        Socket = webSocket;

        var reliableChannel = new WebSocketReliableChannel(webSocket);
        var unreliableChannel = new WebSocketReliableChannel(webSocket);

        _channels = new[] { reliableChannel, unreliableChannel };

        Reliable = reliableChannel;
        Unreliable = unreliableChannel;
    }

    public void Close()
    {
        Socket.CloseAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);
    }
    
    public async Task Flush()
    {
        foreach (var channel in _channels)
        {
            try
            {
                await channel.Flush();
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
            }
        }
    }
}