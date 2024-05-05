using Ragon.Protocol;

namespace Ragon.Client
{
  public class RoomDataHandler: IHandler
  {
    private readonly RagonClient _client;
    public RoomDataHandler(RagonClient client)
    {
      _client = client;
    }
    
    public void Handle(RagonBuffer reader)
    {
      var len = reader.ReadUShort();
      
      _client.Room?.Data(reader);
    }
  }
}