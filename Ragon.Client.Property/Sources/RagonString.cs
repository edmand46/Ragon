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


using System.Text;
using Ragon.Protocol;

namespace Ragon.Client.Property
{
  [Serializable]
  public class RagonString : RagonProperty
  {
    public string Value
    {
      get => _value;
      set
      {
        _value = value;
        
        MarkAsChanged();
      }
    }

    private string _value;
    private readonly UTF8Encoding _utf8Encoding = new UTF8Encoding(false, true);
    
    public RagonString(
      string initialValue,
      bool invokeLocal = true,
      int priority = 0
    ) : base(priority, invokeLocal)
    {
      _value = initialValue;
    }

    public override void Serialize(RagonBuffer buffer)
    {
      var data = _utf8Encoding.GetBytes(_value);
      var len = (uint) data.Length;
      
      buffer.Write(len, 16);
      buffer.WriteBytes(data);
    }

    public override void Deserialize(RagonBuffer buffer)
    {
      var len = (int) buffer.Read(16);
      var data = buffer.ReadBytes(len);
      
      _value = _utf8Encoding.GetString(data);
      
      InvokeChanged();
    }
  }
}