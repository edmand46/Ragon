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

namespace Ragon.Server.Project;

public class ProjectRegistry
{
  private readonly Dictionary<string, RagonProject> _projectsByKey;
  private readonly Dictionary<int, RagonProject> _projectsById;
  private readonly int _connectionLimitPerProject;

  public ProjectRegistry(int connectionLimitPerProject)
  {
    _projectsByKey = new Dictionary<string, RagonProject>();
    _projectsById = new Dictionary<int, RagonProject>();
    _connectionLimitPerProject = connectionLimitPerProject;
  }

  public bool ValidateKey(string key)
  {
    if (string.IsNullOrWhiteSpace(key))
      return false;

    if (key.Length < 4)
      return false;

    return true;
  }

  public RagonProject? GetOrCreateProject(string projectKey)
  {
    if (!_projectsByKey.TryGetValue(projectKey, out var project))
    {
      project = new RagonProject(projectKey);
      _projectsByKey[projectKey] = project;
      _projectsById[project.Id] = project;
    }

    return project;
  }

  public bool CanConnect(string projectKey)
  {
    if (!_projectsByKey.TryGetValue(projectKey, out var project))
      return true;

    return project.ActiveConnections < _connectionLimitPerProject;
  }

  public void RegisterConnection(int projectId)
  {
    if (_projectsById.TryGetValue(projectId, out var project))
    {
      project.IncrementConnections();
    }
  }

  public void UnregisterConnection(int projectId)
  {
    if (_projectsById.TryGetValue(projectId, out var project))
    {
      project.DecrementConnections();

      if (project.ActiveConnections <= 0)
      {
        _projectsByKey.Remove(project.Key);
        _projectsById.Remove(projectId);
      }
    }
  }

  public RagonProject? GetProjectById(int projectId)
  {
    return _projectsById.TryGetValue(projectId, out var project) ? project : null;
  }
}
