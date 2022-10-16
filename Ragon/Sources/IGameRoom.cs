namespace Ragon.Core;

public interface IGameRoom
{
  public string Id { get; }
  public string Map { get; }
  public int PlayersMin { get; }
  public int PlayersMax { get; }
  public int PlayersCount { get; }
  
  public Player GetPlayerById(string id);
  public Player GetPlayerByPeer(ushort peerId);
  public Entity GetEntityById(int entityId);
}