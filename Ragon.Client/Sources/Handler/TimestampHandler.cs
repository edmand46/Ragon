using Ragon.Protocol;

namespace Ragon.Client;

public class TimestampHandler: IHandler
{
  private readonly RagonClient _client;
  public TimestampHandler(RagonClient client)
  {
    _client = client;
  }
  
  public void Handle(RagonStream buffer)
  {
    var timestamp0 = (uint)buffer.ReadInt(); 
    var timestamp1 = (uint)buffer.ReadInt();
    var value = new DoubleToUInt { Int0 = timestamp0, Int1 = timestamp1 };
    
    _client.SetTimestamp(value.Double);
  }
}