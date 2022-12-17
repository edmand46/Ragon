namespace Ragon.Server;

public interface INetworkConnection
{
    public ushort Id { get; }
    public INetworkChannel ReliableChannel { get; }
    public INetworkChannel UnreliableChannel { get; }
}