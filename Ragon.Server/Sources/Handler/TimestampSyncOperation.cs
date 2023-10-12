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
using Ragon.Server.IO;

namespace Ragon.Server.Handler;

public class TimestampSyncOperation: BaseOperation
{
  public TimestampSyncOperation(RagonBuffer reader, RagonBuffer writer) : base(reader, writer)
  {
  }

  public override void Handle(RagonContext context, NetworkChannel channel)
  {
    var timestamp0 = Reader.Read(32);
    var timestamp1 = Reader.Read(32);
    var value = new DoubleToUInt() { Int0 = timestamp0, Int1 = timestamp1 };
    
    context.RoomPlayer?.SetTimestamp(value.Double);
  }
}