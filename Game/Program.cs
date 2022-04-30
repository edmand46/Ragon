using System;
using Game.Source;
using NetStack.Serialization;
using Ragon.Core;

namespace Game
{
    class Program
    {
        static void Main(string[] args)
        {
            var bootstrap = new Bootstrap();
            bootstrap.Configure(new GameFactory());

            Console.Read();
        }
    }
}