namespace Ragon.Core;

public interface IGameRoom
{
  public string Id { get; }
  public string Map { get; }
  public int PlayersMin { get; }
  public int PlayersMax { get; }
  public int PlayersCount { get; }
  
  public Entity GetEntityById(int entityId);
}