using Ragon.Client;
using Ragon.Client.Simulation;

namespace Ragon.Simulation;

public class EntityListener : IRagonEntityListener
{
  public void OnEntityCreated(RagonEntity entity)
  {
    var health = new RagonFloat(100.0f, false, 0);
    health.Value = 50;
    health.Changed += () => Console.WriteLine($"[Ragon Property] Another Health: {health.Value}");

    var points = new RagonInt(0, -1000, 1000, false, 0);
    points.Changed += () => Console.WriteLine($"[Ragon Property] Anther Points: {points.Value}");

    var name = new RagonString("Eduard", false);
    name.Changed += () => Console.WriteLine($"[Ragon Property] Another Name: {name.Value}");

    entity.State.AddProperty(health);
    entity.State.AddProperty(points);
    entity.State.AddProperty(name);
  }
}

public class Simulation
{
  public void Start()
  {
    var client = new Ragon.Simulation.Client();
    client.Start();

    // INetworkConnection protocol = debug ? new RagonNullConnection() : new RagonENetConnection();
    // var network = new RagonClient(protocol, new EntityListener(), 30);
    // var game = new Game(network);
    // network.AddListener(game);
    // network.Connect("127.0.0.1", 5001, "1.0.0");
    //   var dt = 1000 / 60.0f;
    //   while (true)
    //   {
    //     game.Update();
    //     network.Update(dt);
    //     Thread.Sleep((int) dt);
    //   }
    //   
    //   network.Disconnect();
  }
}