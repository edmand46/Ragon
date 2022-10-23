using System;
using System.Threading;
using Game.Source;
using Ragon.Core;

namespace SimpleServer
{
    class Program
    {
        static void Main(string[] args)
        {
            var bootstrap = new Bootstrap();
            var app = bootstrap.Configure(new SimplePluginFactory());
            app.Start();
            Console.Read();
            app.Stop();
        }
    }
}