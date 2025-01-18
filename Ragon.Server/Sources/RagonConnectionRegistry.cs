namespace Ragon.Server;

public class RagonConnectionRegistry
{
  private readonly Dictionary<ushort, RagonContext> _contextsByConnection = new();
  private readonly Dictionary<string, RagonContext> _contextsByPlayerId = new();
  private readonly List<RagonContext> _contexts = new();
  private readonly List<RagonContext> _playerContexts = new();

  public void Add(string playerId, RagonContext context)
  {
    _contextsByPlayerId.Add(playerId, context);
    _playerContexts.Add(context);
  }

  public void Add(ushort connectionId, RagonContext context)
  {
    _contextsByConnection.Add(connectionId, context);
    _contexts.Add(context);
  }


  public bool Remove(ushort connectionId, out RagonContext o)
  {
    if (_contextsByConnection.Remove(connectionId, out var context))
    {
      _contexts.Remove(context);
      o = context;

      return true;
    }

    o = null;

    return false;
  }
  
  public bool Remove(string playerId)
  {
    if (_contextsByPlayerId.Remove(playerId, out var context))
    {
      _playerContexts.Remove(context);
      
      return true;
    }

    return false;
  }

  public bool TryGetValue(ushort connectionId, out RagonContext o)
  {
    return _contextsByConnection.TryGetValue(connectionId, out o);
  }

  public IReadOnlyList<RagonContext> Contexts => _contexts;
  public IReadOnlyList<RagonContext> PlayerContexts => _playerContexts;
  
  public RagonContext? GetContextByConnectionId(ushort peerId) => _contextsByConnection.GetValueOrDefault(peerId);
  public RagonContext? GetContextById(string playerId) => _contextsByPlayerId.GetValueOrDefault(playerId);
}