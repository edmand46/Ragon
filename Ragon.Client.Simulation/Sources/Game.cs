

namespace Ragon.Client.Simulation;

public class Game : IRagonListener
{
    private RagonFloat _health;
    private RagonInt _points;
    private RagonString _name;
    private RagonEntity _entity;
    private RagonClient _client;

    public Game(RagonClient client)
    {
        _client = client;
    }

    public void OnConnected(RagonClient client)
    {
        RagonLog.Trace("Connected");
        _client.Session.AuthorizeWithKey("defaultkey", "Player Eduard");
    }

    public void OnAuthorizationSuccess(RagonClient client, string playerId, string playerName)
    {
        RagonLog.Trace("Authorized");
        client.Session.CreateOrJoin("Example", 1, 20);
    }

    public void OnAuthorizationFailed(RagonClient client, string message)
    {
        Console.WriteLine($"Authorization failed: {message}");
    }

    public void OnJoined(RagonClient client)
    {
        RagonLog.Trace("Joined");

        _health = new RagonFloat(100.0f, false, 0);
        _health.Changed += () => Console.WriteLine($"[Ragon Property] Health: {_health.Value}");

        _points = new RagonInt(0, -1000, 1000, false, 0);
        _points.Changed += () => Console.WriteLine($"[Ragon Property] Points: {_points.Value}");

        _name = new RagonString("Edmand 000", false);
        _name.Changed += () => Console.WriteLine($"[Ragon Property] Name: {_name.Value}");

        _entity = new RagonEntity(12, 0);
        _entity.State.AddProperty(_health);
        _entity.State.AddProperty(_points);
        _entity.State.AddProperty(_name);

        client.Room.CreateEntity(_entity);
    }

    public void OnFailed(RagonClient client, string message)
    {
        RagonLog.Trace("Failed to join");
    }

    public void OnLeft(RagonClient client)
    {
        RagonLog.Trace("Left");
    }

    public void OnDisconnected(RagonClient client)
    {
        RagonLog.Trace("Disconnected");
    }

    public void OnPlayerJoined(RagonClient client, RagonPlayer player)
    {
        RagonLog.Trace("Player joined");
    }

    public void OnPlayerLeft(RagonClient client, RagonPlayer player)
    {
        RagonLog.Trace("Player left");
    }

    public void OnOwnershipChanged(RagonClient client, RagonPlayer player)
    {
        RagonLog.Trace("Owner ship changed");
    }

    public void OnLevel(RagonClient client, string sceneName)
    {
        RagonLog.Trace($"New level: {sceneName}");

        client.Room.SceneLoaded();
    }

    private float _timer = 0;

    public void Update()
    {
        if (_client.Status != RagonStatus.ROOM)
            return;

        _timer += 1 / 60.0f;
        if (_timer > 1)
        {
            _health.Value += 20.0f;
            _points.Value += 10;
            _name.Value = $"Edmand 00{_client.Room.Local.PeerId}";
            Console.WriteLine($"{_health.Value} {_points.Value} {_name.Value}");
            _timer = 0;
        }
    }
}