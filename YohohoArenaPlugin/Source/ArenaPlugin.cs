using NLog;
using Ragon.Core;

namespace Game.Source
{
  public class ArenaPlugin: PluginBase
  {
    private ILogger _logger = LogManager.GetCurrentClassLogger();
    
    public override void OnStart()
    {
      // _logger.Info("Plugin started");  
    }

    public override void OnStop()
    {
      // _logger.Info("Plugin stopped");
    }

    public long FindPrimeNumber(int n)
    {
      int count=0;
      long a = 2;
      while(count<n)
      {
        long b = 2;
        int prime = 1;// to check if found a prime
        while(b * b <= a)
        {
          if(a % b == 0)
          {
            prime = 0;
            break;
          }
          b++;
        }
        if(prime > 0)
        {
          count++;
        }
        a++;
      }
      return (--a);
    }
  }
}