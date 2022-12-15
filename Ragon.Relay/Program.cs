using System;
using System.Runtime.InteropServices;
using NLog;
using Ragon.Core;

namespace Ragon.Relay
{
    [StructLayout(LayoutKind.Sequential)]
    struct Serializer
    {
        [FieldOffset(0)] public Guid Uuid;
        [FieldOffset(0)] public long Long0;
        [FieldOffset(0)] public long Long1;
    }
    class Program
    {
        static void Main(string[] args)
        {
            var logger = LogManager.GetLogger("Ragon.Relay");
            logger.Info("Relay Application");
            var configuration = Configuration.Load("config.json");
            var relay = new Application(configuration);
            relay.Start();
            logger.Info("Started");
            Console.ReadKey();
            relay.Stop();
            logger.Info("Stopped");
        }
    }
}