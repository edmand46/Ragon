using ENet;

namespace Ragon.Server.ENet;

public sealed class ENetConnection: INetworkConnection
{
  public ushort Id { get; }
  public INetworkChannel Reliable { get; private set; }
  public INetworkChannel Unreliable { get; private set; }
  
  public ENetConnection(Peer peer)
  {
    Id = (ushort) peer.ID;
    Reliable = new ENetReliableChannel(peer, 0);
    Unreliable = new ENetUnreliableChannel(peer, 1);
  }
}