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

using NLog;
using System.Net;
using System.Text.Json;
using Ragon.Server.IO;
using Ragon.Server.Plugin;

namespace Ragon.Server.Http;

public class RagonHttpServer
{
  private readonly ILogger _logger = LogManager.GetCurrentClassLogger();
  private readonly IExecutor _executor;
  private readonly IServerPlugin _serverPlugin;
  private HttpListener _httpListener;
  private CancellationTokenSource _cancellationTokenSource;

  public RagonHttpServer(IExecutor executor, IServerPlugin serverPlugin)
  {
    _serverPlugin = serverPlugin;
    _executor = executor;
  }

  public async void StartAccept(CancellationToken cancellationToken)
  {
    while (!cancellationToken.IsCancellationRequested)
    {
      var context = await _httpListener.GetContextAsync();

      if (context.Request.HttpMethod != "POST")
      {
        context.Response.StatusCode = 404;
        context.Response.ContentLength64 = 0;
        context.Response.Close();
      }

      var request = context.Request;
      var reader = new StreamReader(request.InputStream, request.ContentEncoding);
      var rawJson = await reader.ReadToEndAsync();
      var httpCommand = JsonDocument.Parse(rawJson);
      if (httpCommand != null)
      {
        try
        {
          var command = httpCommand.RootElement.GetProperty("command");
          var payload = httpCommand.RootElement.GetProperty("payload");

          if (_serverPlugin.OnCommand(command.GetString() ?? "none", payload.GetRawText()))
          {
            context.Response.StatusCode = 200;
            context.Response.ContentLength64 = 0;
            context.Response.Close(); 
          }
          else
          {
            context.Response.StatusCode = 403;
            context.Response.ContentLength64 = 0;
            context.Response.Close();
          }
        }
        catch (Exception ex)
        {
          _logger.Error(ex);
          
          context.Response.StatusCode = 505;
          context.Response.ContentLength64 = 0;
          context.Response.Close();
        }
        
        continue;
      }

      context.Response.StatusCode = 403;
      context.Response.ContentLength64 = 0;
      context.Response.Close();
    }
  }

  public void Start(RagonServerConfiguration configuration)
  {
    _cancellationTokenSource = new CancellationTokenSource();
    _logger.Info($"Listen at http://0.0.0.0:{configuration.HttpPort}/");

    _httpListener = new HttpListener();
    _httpListener.Prefixes.Add($"http://127.0.0.1:{configuration.HttpPort}/");
    _httpListener.Start();

    _executor.Run(() => StartAccept(_cancellationTokenSource.Token), TaskCreationOptions.LongRunning);
  }

  public void Stop()
  {
    _cancellationTokenSource.Cancel();
    _httpListener.Stop();
  }
}