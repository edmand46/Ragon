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

namespace Ragon.Client
{
  public class RagonLog
  {
    private static IRagonLogger _ragonLogger;
    static RagonLog() => _ragonLogger = new RagonConsoleLogger();
    public static void Set(IRagonLogger logger) => _ragonLogger = logger;
    public static void Warn(string message) => _ragonLogger.Warn(message);
    public static void Trace(string message) => _ragonLogger.Trace(message);
    public static void Info(string message) => _ragonLogger.Info(message);
    public static void Error(string message) => _ragonLogger.Error(message);
  }
}