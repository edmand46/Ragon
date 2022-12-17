using System;
using System.Runtime.InteropServices;
using NLog;
using Ragon.Core;

namespace Ragon.Relay
{
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