using System;
using System.Runtime.CompilerServices;
using NetStack.Buffers;

namespace Ragon.Common.Protocol
{
  public static class ProtocolHeader
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteOperation(ushort id, byte[] data) {
      data[0] = (byte)(id & 0x00FF);
      data[1] = (byte)((id & 0xFF00) >> 8);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ushort ReadOperation(byte[] data, int offset = 0)
    {
      return (ushort)(data[offset] + (data[offset + 1] << 8));
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteEntity(int id, byte[] data, int offset = 2) {
      data[offset] = (byte)(id & 0x00FF);
      data[offset + 1] = (byte)((id & 0xFF00) >> 8);
      data[offset + 2] = (byte)((id & 0xFF00) >> 16);
      data[offset + 3] = (byte)((id & 0xFF00) >> 24);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ReadEntity(byte[] data, int offset = 2)
    {
      return (ushort)(data[offset] + (data[offset + 1] << 8) + (data[offset + 2] << 16) + (data[offset + 3] << 24));
    }

    public static int ReadProperty(byte[] data, int offset = 2)
    {
      return ReadEntity(data, offset);
    }

    public static void WriteProperty(int id, byte[] data)
    {
      WriteEntity(id, data);
    }
    
    public static int ReadEvent(byte[] data, int offset = 2)
    {
      return ReadEntity(data, offset);
    }

    public static void WriteEvent(int id, byte[] data)
    {
      WriteEntity(id, data);
    }
  }
}