using Ragon.Protocol;

namespace Ragon.Server.Entity;

public ref struct RagonEntityParameters
{
  public ushort Type;
  public ushort StaticId;
  public ushort AttachId;
  public RagonAuthority Authority;
}