using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ENet;
using NLog;

namespace Ragon.Core
{
  public class Application
  {
    private readonly Logger _logger = LogManager.GetCurrentClassLogger();
    private readonly GameThread _gameThread;

    public Application(PluginFactory factory, Configuration configuration)
    {
      ThreadPool.SetMinThreads(1, 1);
      
      _gameThread = new GameThread(factory, configuration);
    }
    
    public void Start()
    {
      Library.Initialize();
      
      _gameThread.Start();
    }

    public void Stop()
    {
      _gameThread.Stop();
      
      Library.Deinitialize();
    }
  }
}