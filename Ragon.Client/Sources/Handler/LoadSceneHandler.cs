﻿/*
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

internal class SceneLoadHandler: IHandler
{
  private readonly RagonClient _client;
  private readonly RagonListenerList _listenerList;
  
  public SceneLoadHandler(
    RagonClient ragonClient,
    RagonListenerList listenerList
    )
  {
    _client = ragonClient;
    _listenerList = listenerList;
  }
  
  public void Handle(RagonBuffer reader)
  {
    var sceneName = reader.ReadString();
    var room = _client.Room;
    
    room.Cleanup();
    room.Update(sceneName);
    
    _listenerList.OnSceneRequest(sceneName);
  }
}