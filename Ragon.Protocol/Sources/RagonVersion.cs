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


namespace Ragon.Protocol
{
  public static class RagonVersion
  {
    public static uint Parse(string version)
    {
      var strings = version.Split(".");
      if (strings.Length < 3)
        return 0;
      
      var parts = new uint[] {0, 0, 0};
      for (int i = 0; i < parts.Length; i++)
      {
        if (!uint.TryParse(strings[i], out var v))
          return 0;

        parts[i] = v;
      }
      
      return (parts[0] << 16) | (parts[1] << 8) | parts[2];
    }

    public static string Parse(uint version)
    {
      return (version >> 16 & 0xFF) + "." + (version >> 8 & 0xFF) + "." + (version & 0xFF);
    }
  }
}