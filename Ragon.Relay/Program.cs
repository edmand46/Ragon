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
            
            var configuration = Configuration.Load("relay.config.json");
            var relay = new Application(configuration);
            
            logger.Info("Started");
            relay.Start();

            Console.ReadKey();
            
            relay.Stop();
            logger.Info("Stopped");
        }
    }
}