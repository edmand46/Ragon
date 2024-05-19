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

using Ragon.Server.Entity;

namespace Ragon.Server;

public class RagonEntityCache
{
  private readonly List<RagonEntity> _dynamicEntitiesList = new List<RagonEntity>();
  private readonly List<RagonEntity> _staticEntitiesList = new List<RagonEntity>();
  private readonly Dictionary<ushort, RagonEntity> _entitiesMap = new Dictionary<ushort, RagonEntity>();

  public IReadOnlyList<RagonEntity> StaticList => _staticEntitiesList;
  public IReadOnlyList<RagonEntity> DynamicList => _dynamicEntitiesList;
  public IReadOnlyDictionary<ushort, RagonEntity> Map => _entitiesMap;

  public void Add(RagonEntity entity)
  {
    if (entity.StaticId != 0)
      _staticEntitiesList.Add(entity);
    else
      _dynamicEntitiesList.Add(entity);
    
    _entitiesMap.Add(entity.Id, entity);
  }

  public bool Remove(RagonEntity entity)
  {
    if (_entitiesMap.Remove(entity.Id, out var existEntity))
    {
      _staticEntitiesList.Remove(entity);
      _dynamicEntitiesList.Remove(entity);
      
      return true;
    }
    return false;
  }
}