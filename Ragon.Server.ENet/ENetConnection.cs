using ENet;

namespace Ragon.Server.ENet;

public sealed class ENetConnection: INetworkConnection
{
  public ushort Id { get; }
  public INetworkChannel ReliableChannel { get; private set; }
  public INetworkChannel UnreliableChannel { get; private set; }
  
  public ENetConnection(Peer peer)
  {
    Id = (ushort) peer.ID;
    ReliableChannel = new ENetReliableChannel(peer, 0);
    UnreliableChannel = new ENetUnreliableChannel(peer, 1);
  }
}