namespace Ragon.Core.Game;

public class EntityList
{
  private List<Entity> _dynamicEntitiesList = new List<Entity>();
  private List<Entity> _staticEntitesList = new List<Entity>();
  private Dictionary<ushort, Entity> _entitiesMap = new Dictionary<ushort, Entity>();

  public IReadOnlyList<Entity> StaticList => _staticEntitesList;
  public IReadOnlyList<Entity> DynamicList => _dynamicEntitiesList;
  public IReadOnlyDictionary<ushort, Entity> Map => _entitiesMap;

  public void Add(Entity entity)
  {
    if (entity.StaticId != 0)
      _staticEntitesList.Add(entity);
    else
      _dynamicEntitiesList.Add(entity);
    
    _entitiesMap.Add(entity.Id, entity);
  }

  public Entity Remove(Entity entity)
  {
    if (_entitiesMap.Remove(entity.Id, out var existEntity))
    {
      _staticEntitesList.Remove(entity);
      _dynamicEntitiesList.Remove(entity);
      
      return existEntity;
    }

    return null;
  }
}