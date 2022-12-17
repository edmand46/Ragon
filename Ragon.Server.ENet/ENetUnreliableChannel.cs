using ENet;

namespace Ragon.Server.ENet;

public sealed class ENetUnreliableChannel: INetworkChannel
{
  private Peer _peer;
  private byte _channelId;
  
  public ENetUnreliableChannel(Peer peer, int channelId)
  {
    _peer = peer;
    _channelId = (byte) channelId;
  }
  
  public void Send(byte[] data)
  {
    var newPacket = new Packet();
    newPacket.Create(data, data.Length, PacketFlags.None);

    _peer.Send(_channelId, ref newPacket);
  }
}