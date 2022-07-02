namespace Ragon.Core;

public interface IGameRoom
{
  public string Id { get; }
  public string Map { get; }
  public int PlayersMin { get; }
  public int PlayersMax { get; }
  public int PlayersCount { get; }
  
  public Player GetPlayerById(uint peerId);
  public Entity GetEntityById(int entityId);
  public Player GetOwner();
  public IDispatcher GetThreadDispatcher();
  public IScheduler GetScheduler();
  
  public void Send(uint peerId, byte[] rawData, DeliveryType deliveryType = DeliveryType.Unreliable);
  public void Broadcast(uint[] peersIds, byte[] rawData, DeliveryType deliveryType = DeliveryType.Unreliable);
  public void Broadcast(byte[] rawData, DeliveryType deliveryType = DeliveryType.Unreliable);
}