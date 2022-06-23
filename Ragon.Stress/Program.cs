using System;
using ENet;
using Ragon.Common;

namespace Stress
{
  class SimulationClient
  {
    public Host Host;
    public Peer Peer;
    public bool InRoom;
    public List<int> Entities = new List<int>();
  }

  class SimulationThread
  {
    private List<SimulationClient> _clients = new List<SimulationClient>();

    public void Start(string url, ushort port, int numClients)
    {
      for (var i = 0; i < numClients; i++)
      {
        var client = CreateClient(url, port);
        _clients.Add(client);
      }

      var thread = new Thread(Execute);
      thread.IsBackground = true;
      thread.Start();
    }

    public void Execute()
    {
      var ragonSerializer = new RagonSerializer();

      while (true)
      {
        foreach (SimulationClient simulationClient in _clients)
        {
          bool polled = false;
          Event netEvent;

          while (!polled)
          {
            if (simulationClient.Host.CheckEvents(out netEvent) <= 0)
            {
              if (simulationClient.Host.Service(0, out netEvent) <= 0)
                break;

              polled = true;
            }

            switch (netEvent.Type)
            {
              case EventType.None:
                break;
              case EventType.Connect:
              {
                ragonSerializer.Clear();
                ragonSerializer.WriteOperation(RagonOperation.AUTHORIZE);
                ragonSerializer.WriteString("defaultkey");
                ragonSerializer.WriteString("Player " + DateTime.Now.Ticks);
                ragonSerializer.WriteByte(0);

                var sendData = ragonSerializer.ToArray();
                var packet = new Packet();
                packet.Create(sendData, PacketFlags.Reliable);
                simulationClient.Peer.Send(0, ref packet);
                Console.WriteLine("Client connected to server");
                break;
              }
              case EventType.Disconnect:
                Console.WriteLine("Client disconnected from server");
                break;
              case EventType.Timeout:
                Console.WriteLine("Client connection timeout");
                break;
              case EventType.Receive:
                var data = new byte[netEvent.Packet.Length];
                netEvent.Packet.CopyTo(data);

                var op = (RagonOperation) data[0];
                switch (op)
                {
                  case RagonOperation.AUTHORIZED_SUCCESS:
                  {
                    ragonSerializer.Clear();
                    ragonSerializer.WriteOperation(RagonOperation.JOIN_OR_CREATE_ROOM);
                    ragonSerializer.WriteInt(2);
                    ragonSerializer.WriteInt(20);
                    ragonSerializer.WriteString("map");
                    
                    var sendData = ragonSerializer.ToArray();
                    var packet = new Packet();
                    packet.Create(sendData, PacketFlags.Reliable);
                    simulationClient.Peer.Send(0, ref packet);
                    break;
                  }
                  case RagonOperation.JOIN_SUCCESS:
                  {
                    simulationClient.InRoom = true;
                    
                    ragonSerializer.Clear();
                    ragonSerializer.WriteOperation(RagonOperation.SCENE_IS_LOADED);
                    
                    var sendData = ragonSerializer.ToArray();
                    var packet = new Packet();
                    packet.Create(sendData, PacketFlags.Reliable);
                    simulationClient.Peer.Send(0, ref packet);
                    
                    break;
                  }
                  case RagonOperation.SNAPSHOT:
                  {
                    ragonSerializer.Clear();
                    ragonSerializer.WriteOperation(RagonOperation.CREATE_ENTITY);
                    ragonSerializer.WriteUShort(0);
                    ragonSerializer.WriteUShort(0);
                    ragonSerializer.WriteUShort(0);
                    
                    var sendData = ragonSerializer.ToArray();
                    var packet = new Packet();
                    packet.Create(sendData, PacketFlags.Reliable);
                    simulationClient.Peer.Send(0, ref packet);
                    break;
                  }
                  case RagonOperation.CREATE_ENTITY:
                  {
                    ReadOnlySpan<byte> payload = data.AsSpan().Slice(1, data.Length - 1);
                    ragonSerializer.Clear();
                    ragonSerializer.FromSpan(ref payload);

                    var entityType = ragonSerializer.ReadUShort();
                    var state = ragonSerializer.ReadByte();
                    var ennt = ragonSerializer.ReadByte();
                    var entityId = ragonSerializer.ReadInt();
                    
                    simulationClient.Entities.Add(entityId);
                    break;
                  }
                }
                Console.WriteLine(op);
                // Console.WriteLine("Packet received from server - Channel ID: " + netEvent.ChannelID + ", Data length: " + netEvent.Packet.Length);
                netEvent.Packet.Dispose();
                break;
            }
          }

          if (simulationClient.InRoom)
          {
            foreach (var entity in simulationClient.Entities)
            {
              ragonSerializer.Clear();
              ragonSerializer.WriteOperation(RagonOperation.REPLICATE_ENTITY_STATE);
              ragonSerializer.WriteInt(entity);
              ragonSerializer.WriteInt(100);
              ragonSerializer.WriteInt(200);
              ragonSerializer.WriteInt(300);
                    
              var sendData = ragonSerializer.ToArray();
              var packet = new Packet();
              packet.Create(sendData, PacketFlags.Instant);
              simulationClient.Peer.Send(1, ref packet);
            }
          }
        }

        Thread.Sleep(16);
      }
    }

    SimulationClient CreateClient(string url, ushort port)
    {
      Host client = new Host();
      Address address = new Address();

      address.SetHost(url);
      address.Port = port;

      client.Create();
      Console.WriteLine("Created client");

      Peer peer = client.Connect(address);

      return new SimulationClient() {Host = client, Peer = peer};
    }
  }

  class Program
  {
    static void Main(string[] args)
    {
      Library.Initialize();
   
      
      {
        var thread = new SimulationThread();
        thread.Start("127.0.0.1", 4444, 250);
      }
      
      Thread.Sleep(3000);
      
      {
        var thread = new SimulationThread();
        thread.Start("127.0.0.1", 4444, 250);
      }
      
      Thread.Sleep(3000);
      
      
      {
        var thread = new SimulationThread();
        thread.Start("127.0.0.1", 4444, 250);
      }
      
      Thread.Sleep(3000);
      
      {
        var thread = new SimulationThread();
        thread.Start("127.0.0.1", 4444, 250);
      }
      
      Thread.Sleep(3000);
      
      {
        var thread = new SimulationThread();
        thread.Start("127.0.0.1", 4444, 250);
      }
      
      Thread.Sleep(3000);
      
      {
        var thread = new SimulationThread();
        thread.Start("127.0.0.1", 4444, 250);
      }
      
      Thread.Sleep(3000);
      
      {
        var thread = new SimulationThread();
        thread.Start("127.0.0.1", 4444, 250);
      }
      
      Thread.Sleep(3000);
      
      {
        var thread = new SimulationThread();
        thread.Start("127.0.0.1", 4444, 250);
      }
      
      Thread.Sleep(3000);
      

      Console.ReadKey();
      Library.Deinitialize();
    }
  }
}