namespace Ragon.Server;

public class RagonContextObserver
{
  private Dictionary<string, RagonContext> _contexts;
  public RagonContextObserver(Dictionary<string, RagonContext> contexts)
  {
    _contexts = contexts;
  }

  public void OnAuthorized(RagonContext context) 
  {
    _contexts.Add(context.LobbyPlayer.Id, context);
  }
}