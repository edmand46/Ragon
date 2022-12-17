using System.Diagnostics;
using ENet;
using NLog;
using Ragon.Common;


namespace Ragon.Server.ENet
{
  public sealed class ENetServer: INetworkServer 
  {
    public ENetConnection[] Connections;
    private ILogger _logger = LogManager.GetCurrentClassLogger();
    private INetworkListener _listener;
    private uint _protocol;
    private Host _host;
    private Event _event;
    private NetworkConfiguration _configuration;

    public ENetServer()
    {
      _host = new Host();
    }

    public void Start(INetworkListener listener, NetworkConfiguration configuration)
    {
      Library.Initialize();
      
      _listener = listener;
      _protocol = configuration.Protocol;
      
      Connections = new ENetConnection[configuration.LimitConnections];
      
      var address = new Address { Port = (ushort) configuration.Port };
      _host.Create(address, Connections.Length, 2, 0, 0, 1024 * 1024);

      var protocolDecoded = RagonVersion.Parse(_protocol);
      _logger.Info($"Network listening on {configuration.Port}");
      _logger.Info($"Protocol: {protocolDecoded}");
    }

    public void Poll()
    {
      bool polled = false;
      while (!polled)
      {
        if (_host.CheckEvents(out _event) <= 0)
        {
          if (_host.Service(0, out _event) <= 0)
            break;

          polled = true;
        }

        switch (_event.Type)
        {
          case EventType.None:
          {
            _logger.Trace("None event");
            break;
          }
          case EventType.Connect:
          {
            if (IsValidProtocol(_event.Data))
            {
              _logger.Warn("Mismatched protocol, close connection");
              break;
            }

            var connection = new ENetConnection(_event.Peer);
            Connections[_event.Peer.ID] = connection;
            
            _listener.OnConnected(connection);
            break;
          }
          case EventType.Disconnect:
          {
            var connection = Connections[_event.Peer.ID];
            _listener.OnDisconnected(connection);
            break;
          }
          case EventType.Timeout:
          {
            var connection = Connections[_event.Peer.ID];
            _listener.OnTimeout(connection);
            break;
          }
          case EventType.Receive:
          {
            var peerId = (ushort) _event.Peer.ID;
            var connection = Connections[peerId];
            var dataRaw = new byte[_event.Packet.Length];
            
            _event.Packet.CopyTo(dataRaw);
            _event.Packet.Dispose();
            
            _listener.OnData(connection, dataRaw);
            break;
          }
        }
      }
    }

    public void Stop()
    {
      _host?.Dispose();
      
      Library.Deinitialize();
    }

    private bool IsValidProtocol(uint protocol)
    {
      return protocol == _configuration.Protocol;
    }
  }
}