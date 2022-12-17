using Ragon.Common;

namespace Ragon.Core;

public interface IHandler
{
  public void Handle(PlayerContext context, RagonSerializer reader, RagonSerializer writer);
}