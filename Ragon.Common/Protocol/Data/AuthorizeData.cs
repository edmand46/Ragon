using NetStack.Serialization;

namespace Ragon.Core
{

  public class AuthorationData : IData
  {
    public string Login { get; set; }
    public string Password { get; set; }

    public void Serialize(BitBuffer buffer)
    {
      buffer.AddString(Login);
      buffer.AddString(Password);
    }

    public void Deserialize(BitBuffer buffer)
    {
      Login = buffer.ReadString();
      Password = buffer.ReadString();
    }
  }
}