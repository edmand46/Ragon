using ENet;

namespace Ragon.Server.ENet;

public sealed class ENetReliableChannel: INetworkChannel
{
  private Peer _peer;
  private byte _channelId;
  
  public ENetReliableChannel(Peer peer, int channelId)
  {
    _peer = peer;
    _channelId = (byte) channelId;
  }
  
  public void Send(byte[] data)
  {
    var newPacket = new Packet();
    newPacket.Create(data, data.Length, PacketFlags.Reliable);

    _peer.Send(_channelId, ref newPacket);
  }
}