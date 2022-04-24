using System;
using NetStack.Serialization;

namespace Ragon.Common
{
  public static class BitBufferExtension
  {
    public static BitBuffer AddBytes(this BitBuffer buffer, byte[] data)
    {
      buffer.AddInt(data.Length);
      foreach (var b in data)
        buffer.AddByte(b);

      return buffer;
    }
    
    public static byte[] ReadBytes(this BitBuffer buffer)
    {
      var size = buffer.ReadInt();
      var data = new byte[size];
      var i = 0;
      
      while (i < size)
        data[i] = buffer.ReadByte();

      return data;
    }
  }
}