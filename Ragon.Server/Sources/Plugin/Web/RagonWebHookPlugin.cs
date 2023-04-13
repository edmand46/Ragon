using System.Net;
using System.Net.Http.Json;
using Newtonsoft.Json;
using Ragon.Protocol;
using Ragon.Server.Handler;
using Ragon.Server.Lobby;
using Ragon.Server.Room;

namespace Ragon.Server.Plugin.Web;

public class RagonWebHookPlugin
{
  private Dictionary<string, string> _webHooks;

  private RagonServer _server;
  private HttpClient _httpClient;

  public RagonWebHookPlugin(RagonServer server, Configuration configuration)
  {
    _webHooks = new Dictionary<string, string>(configuration.WebHooks);
    _httpClient = new HttpClient();
    _server = server;
  }

  public bool RequestAuthorization(RagonContext context, string name, string password)
  {
    if (_webHooks.TryGetValue("authorization-request", out var value))
    {
      var httpContent = new StringContent("");
      var executor = context.Executor;
      executor.Run(async () =>
      {
        var authorizationOperation = (AuthorizationOperation) _server.ResolveOperation(RagonOperation.AUTHORIZE);
        var response = await _httpClient.PostAsync(new Uri(value), httpContent);
        if (response.StatusCode != HttpStatusCode.OK)
        {
          authorizationOperation.Reject(context);
          return;
        }

        var content = await response.Content.ReadAsStringAsync();
        var authorizationResponse = JsonConvert.DeserializeObject<AuthorizationResponse>(content);
        if (authorizationResponse != null)
        {
          var lobbyPlayer = new RagonLobbyPlayer(context.Connection, authorizationResponse.Id, authorizationResponse.Name, authorizationResponse.Payload);

          context.SetPlayer(lobbyPlayer);
          authorizationOperation.Approve(context);
        }
        else
        {
          authorizationOperation.Reject(context);
        }
      });
      return true;
    }

    return false;
  }

  public void RoomCreated(RagonContext context, RagonRoom room, RagonRoomPlayer player)
  {
    if (_webHooks.TryGetValue("room-created", out var value) && !string.IsNullOrEmpty(value))
    {
      var request = new RoomCreatedRequest()
      {
        Room = new RoomDto(room),
        Player = new PlayerDto(player)
      };
      var content = JsonContent.Create(request);
      var executor = context.Executor;
      executor.Run(() => _httpClient.PostAsync(new Uri(value), content, CancellationToken.None));
    }
  }

  public void RoomRemoved(RagonContext context, RagonRoom ragonRoom)
  {
    if (_webHooks.TryGetValue("room-removed", out var value) && !string.IsNullOrEmpty(value))
    {
      var request = new RoomRemovedRequest()
      {
        Room = new RoomDto(ragonRoom)
      };
      var content = JsonContent.Create(request);
      var executor = context.Executor;
      executor.Run(() => _httpClient.PostAsync(new Uri(value), content, CancellationToken.None));
    }
  }

  public void RoomJoined(RagonContext context, RagonRoom existsRoom, RagonRoomPlayer player)
  {
    if (_webHooks.TryGetValue("room-joined", out var value) && !string.IsNullOrEmpty(value))
    {
      var request = new RoomJoinedRequest()
      {
        Room = new RoomDto(existsRoom),
        Player = new PlayerDto(player)
      };
      var content = JsonContent.Create(request);
      var executor = context.Executor;
      executor.Run(() => _httpClient.PostAsync(new Uri(value), content, CancellationToken.None));
    }
  }

  public void RoomLeaved(RagonContext context, RagonRoom room, RagonRoomPlayer roomPlayer)
  {
    if (_webHooks.TryGetValue("room-leaved", out var value) && !string.IsNullOrEmpty(value))
    {
      var request = new RoomLeavedRequest()
      {
        Room = new RoomDto(room),
        Player = new PlayerDto(roomPlayer)
      };
      var content = JsonContent.Create(request);
      var executor = context.Executor;
      executor.Run(() => _httpClient.PostAsync(new Uri(value), content, CancellationToken.None));
    }
  }
}