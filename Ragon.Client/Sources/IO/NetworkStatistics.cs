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

public class NetworkStatistics
{
  private const double Interval = 1.0d;
  private double _upstreamBandwidth = 0d;
  private double _downstreamBandwidth = 0d;
  private double _time = 0d;
  private ulong _upstreamData = 0;
  private ulong _downstreamData = 0;
  private ulong _sent = 0;
  private ulong _received = 0;
  private int _ping;

  public int Ping => _ping;
  public double UpstreamBandwidth => _upstreamBandwidth;
  public double DownstreamBandwidth => _downstreamBandwidth;
  
  public void Update(ulong sent, ulong received, int ping, float dt)
  {
    _sent = sent;
    _received = received;
    _ping = ping;
    
    _time += dt;
    if (_time >= Interval)
    {
      if (_upstreamData > 0)
      {
        _upstreamData = _sent - _upstreamData;
        _upstreamBandwidth = (_upstreamData / _time) / 1000 ;
      }

      if (_downstreamData > 0)
      {
        _downstreamData = _received - _downstreamData;
        _downstreamBandwidth = (_downstreamData / _time) / 1000;
      }

      _upstreamData = _sent;
      _downstreamData = _received;
      _time -= Interval;
    }
  }
}