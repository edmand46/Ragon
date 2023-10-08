using Ragon.Protocol;

namespace Ragon.Client;

public class RoomDataHandler: IHandler
{
  public void Handle(RagonBuffer reader)
  {
    var rawData = reader.RawData;
  }
}