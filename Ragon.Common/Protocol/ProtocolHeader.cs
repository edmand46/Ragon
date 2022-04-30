using System;
using System.Runtime.CompilerServices;
using NetStack.Buffers;

namespace Ragon.Core
{
  public static class ProtocolHeader
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteUShort(ushort id, ref Span<byte> data) {
      data[0] = (byte)(id & 0x00FF);
      data[1] = (byte)((id & 0xFF00) >> 8);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ushort ReadUShort(ref ReadOnlySpan<byte> data)
    {
      return (ushort)(data[0] + (data[1] << 8));
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteInt(int id, ref Span<byte> data) {
      data[0] = (byte)(id & 0x00FF);
      data[1] = (byte)((id & 0xFF00) >> 8);
      data[2] = (byte)((id & 0xFF00) >> 16);
      data[3] = (byte)((id & 0xFF00) >> 24);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ReadInt(ref ReadOnlySpan<byte> data)
    {
      return (ushort)(data[0] + (data[1] << 8) + (data[2] << 16) + (data[3] << 24));
    }
    
  }
}