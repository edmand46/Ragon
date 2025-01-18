using Ragon.Server;
using Ragon.Server.Lobby;
using Ragon.Server.Plugin;
using Ragon.Server.Time;

namespace Ragon.Relay
{
  public class RelayServerPlugin : BaseServerPlugin
  {
    private RelayConfiguration _relayConfiguration;
    private RagonScheduler _scheduler;
    private RagonConnectionRegistry _connectionRegistry;
    private IRagonLobby _lobby;
    private Reporter _reporter;

    public RelayServerPlugin(RelayConfiguration config)
    {
      _relayConfiguration = config;
    }

    public override void OnAttached(IRagonServer server)
    {
      base.OnAttached(server);
      
      _lobby = server.Lobby;
      _connectionRegistry = server.ConnectionRegistry;
      _scheduler = server.Scheduler;
      _reporter = new Reporter(_relayConfiguration, server, "127.0.0.1", 5000);

      server.Scheduler.Run(new RagonActionTimer(() => _reporter.Done(), 1, -1));
    }

    public override bool OnCommand(string command, string payload)
    {
      return true;
    }

    public override IRoomPlugin CreateRoomPlugin(RoomInformation information)
    {
      return new RelayRoomPlugin();
    }
  }
}