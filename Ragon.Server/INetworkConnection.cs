namespace Ragon.Server;

public interface INetworkConnection
{
    public ushort Id { get; }
    public INetworkChannel Reliable { get; }
    public INetworkChannel Unreliable { get; }
}