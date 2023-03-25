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


namespace Ragon.Client;

public interface INetworkConnection: IRagonConnection
{
    public INetworkChannel Reliable { get; }
    public INetworkChannel Unreliable { get; }
    public Action<byte[]> OnData { get; set; }
    public Action OnConnected { get; set; }
    public Action<DisconnectReason> OnDisconnected { get; set; }
    public ulong BytesSent { get; }
    public  ulong BytesReceived { get; }
    public int Ping { get;  }
    public void Prepare();
    public void Connect(string address, ushort port, uint protocol);
    public void Disconnect();
    public void Update();
    public void Dispose();
}