using System;
using Game.Source;
using Ragon.Core;

namespace SimpleServer
{
    class Program
    {
        static void Main(string[] args)
        {
            var bootstrap = new Bootstrap();
            bootstrap.Configure(new SimplePluginFactory());
            
            Console.Read();
        }
    }
}