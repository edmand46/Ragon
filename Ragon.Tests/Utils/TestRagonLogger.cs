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

using Ragon.Client;
using Ragon.Protocol;
using Xunit.Abstractions;

namespace Ragon.Core.Tests;

public class RagonXUnitLogger: IRagonLogger
{
  private ITestOutputHelper _outputHelper;
  public RagonXUnitLogger(ITestOutputHelper outputHelper)
  {
    _outputHelper = outputHelper;
  }
  
  public void Warn(string message)
  {
    _outputHelper.WriteLine($"[Warn] {message}");
  }

  public void Trace(string message)
  {
    _outputHelper.WriteLine($"[Trace] {message}");
  }

  public void Info(string message)
  {
    _outputHelper.WriteLine($"[Info] {message}");
  }

  public void Error(string message)
  {
    _outputHelper.WriteLine($"[Error] {message}");
  }
}