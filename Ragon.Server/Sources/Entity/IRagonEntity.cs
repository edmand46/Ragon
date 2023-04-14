using Ragon.Protocol;
using Ragon.Server.Room;

namespace Ragon.Server.Entity;

public interface IRagonEntity
{
  public ushort Id { get; }
  public ushort Type { get; }
  public ushort StaticId { get; }
  public ushort AttachId { get; }
  public RagonRoomPlayer Owner { get; }
  public RagonAuthority Authority { get; }
  public RagonPayload Payload { get; }
  public IRagonEntityState State { get; }
}