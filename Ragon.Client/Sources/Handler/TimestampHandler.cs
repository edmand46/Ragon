using Ragon.Protocol;

namespace Ragon.Client;

public class TimestampHandler: IHandler
{
  private readonly RagonClient _client;
  public TimestampHandler(RagonClient client)
  {
    _client = client;
  }
  
  public void Handle(RagonBuffer buffer)
  {
    var timestamp0 = buffer.Read(32); 
    var timestamp1 = buffer.Read(32);
    var value = new DoubleToUInt { Int0 = timestamp0, Int1 = timestamp1 };
    
    _client.SetTimestamp(value.Double);
  }
}