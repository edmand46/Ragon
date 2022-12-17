namespace Ragon.Core.Game;

public class EntityList
{
  private readonly List<Entity> _dynamicEntitiesList = new List<Entity>();
  private readonly List<Entity> _staticEntitiesList = new List<Entity>();
  private readonly Dictionary<ushort, Entity> _entitiesMap = new Dictionary<ushort, Entity>();

  public IReadOnlyList<Entity> StaticList => _staticEntitiesList;
  public IReadOnlyList<Entity> DynamicList => _dynamicEntitiesList;
  public IReadOnlyDictionary<ushort, Entity> Map => _entitiesMap;

  public void Add(Entity entity)
  {
    if (entity.StaticId != 0)
      _staticEntitiesList.Add(entity);
    else
      _dynamicEntitiesList.Add(entity);
    
    _entitiesMap.Add(entity.Id, entity);
  }

  public Entity Remove(Entity entity)
  {
    if (_entitiesMap.Remove(entity.Id, out var existEntity))
    {
      _staticEntitiesList.Remove(entity);
      _dynamicEntitiesList.Remove(entity);
      
      return existEntity;
    }

    return null;
  }
}