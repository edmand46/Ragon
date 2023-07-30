namespace Ragon.Client;

public interface IRagonSceneRequestListener
{
  void OnRequestScene(RagonClient client, string sceneName);
}