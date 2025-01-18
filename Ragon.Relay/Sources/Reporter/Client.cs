using System;
using System.Net;
using System.Net.Sockets;
using Ragon.Server.Logging;

namespace Ragon.Relay;

public class Client
{
  private readonly UdpClient _udpClient;
  private readonly IPEndPoint _endpoint;
  private readonly IRagonLogger _logger;
  
  public Client(string host, int port)
  {
    _logger = LoggerManager.GetLogger("Client");
    _udpClient = new UdpClient();
    _endpoint = new IPEndPoint(IPAddress.Parse(host), port);
  }
  
  public void Send(byte[] data)
  {
    try
    {
      _udpClient.BeginSend(data, data.Length, _endpoint, SendCallback, null);
    }
    catch (Exception ex)
    {
      _logger.Error(ex.Message);
    }
  }

  private void SendCallback(IAsyncResult ar)
  {
    try
    {
      _udpClient.EndSend(ar);
    }
    catch (Exception ex)
    {
      _logger.Error(ex.Message);
    }
  }
}