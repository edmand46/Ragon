using StackExchange.Redis;

namespace Ragon.Core.Storage;

public class Storage
{
  private ConnectionMultiplexer _connection;
  
  public Storage(Configuration _configuration)
  {
    _connection = ConnectionMultiplexer.Connect(_configuration.ApiKey);
  }

  public void UpdateEntity(int entityId)
  {
    var db = _connection.GetDatabase();
    
    db.set("entity_", )
  }

  public void UpdatePlayer()
  {
    
  }

  public void UpdateRoom()
  {
    
  }
}
