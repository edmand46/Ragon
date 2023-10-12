﻿/*
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

namespace Ragon.Client;

internal class AuthorizeSuccessHandler: IHandler
{
  private readonly RagonListenerList _listenerList;
  private readonly RagonClient _client;
  
  public AuthorizeSuccessHandler(
    RagonClient client,
    RagonListenerList listenerList)
  {
    _client = client;
    _listenerList = listenerList;
  }
  
  public void Handle(RagonBuffer reader)
  {
    var playerId = reader.ReadString();
    var playerName = reader.ReadString();
    var playerPayload = reader.ReadString();

    _client.SetStatus(RagonStatus.LOBBY);
    _listenerList.OnAuthorizationSuccess(playerId, playerName, playerPayload);
  }
}