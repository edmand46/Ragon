namespace Ragon.Server.Logging
{
  public class LoggerManager
  {
    private static IRagonLoggerFactory _factory = null!;

    public static void SetLoggerFactory(IRagonLoggerFactory loggerFactory)
    {
      _factory = loggerFactory;
    }

    public static IRagonLogger GetLogger(string tag)
    {
      return _factory.GetLogger(tag);
    }
  }
}