using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Ragon.Protocol
{
  [StructLayout(LayoutKind.Explicit)]
  internal struct ValueConverter
  {
    [FieldOffset(0)] public int Int;
    [FieldOffset(0)] public float Float;
    [FieldOffset(0)] public long Long;
    [FieldOffset(0)] public byte Byte0;
    [FieldOffset(1)] public byte Byte1;
    [FieldOffset(2)] public byte Byte2;
    [FieldOffset(3)] public byte Byte3;
    [FieldOffset(4)] public byte Byte4;
    [FieldOffset(5)] public byte Byte5;
    [FieldOffset(6)] public byte Byte6;
    [FieldOffset(7)] public byte Byte7;
  }

  public class RagonStream
  {
    private byte[] _data;
    private int _offset;
    private int _size;

    public int Lenght => _offset;
    public int Size => _size - _offset;

    public RagonStream(int capacity = 256)
    {
      _data = new byte[capacity];
      _offset = 0;
      _size = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Reset()
    {
      _size = _offset;
      _offset = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddOffset(int offset)
    {
      _offset += offset;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int WriteByte(byte value)
    {
      ResizeIfNeed(1);
      _data[_offset] = value;
      _offset += 1;
      return 1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte ReadByte()
    {
      var value = _data[_offset];
      _offset += 1;
      return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int WriteBool(bool value)
    {
      ResizeIfNeed(1);
      _data[_offset] = value ? (byte)1 : (byte)0;
      _offset += 1;
      return 1;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ReadBool()
    {
      var value = _data[_offset];
      _offset += 1;
      return value == 1;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int WriteInt(int value)
    {
      ResizeIfNeed(4);
      var converter = new ValueConverter() { Int = value };
      _data[_offset] = converter.Byte0;
      _data[_offset + 1] = converter.Byte1;
      _data[_offset + 2] = converter.Byte2;
      _data[_offset + 3] = converter.Byte3;
      _offset += 4;
      return 4;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int WriteInt(int value, int offset)
    {
      ResizeIfNeed(4);
      var converter = new ValueConverter() { Int = value };
      _data[offset] = converter.Byte0;
      _data[offset + 1] = converter.Byte1;
      _data[offset + 2] = converter.Byte2;
      _data[offset + 3] = converter.Byte3;
      return 4;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int ReadInt()
    {
      var converter = new ValueConverter
        { Byte0 = _data[_offset], Byte1 = _data[_offset + 1], Byte2 = _data[_offset + 2], Byte3 = _data[_offset + 3] };
      _offset += 4;
      return converter.Int;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteLong(long value)
    {
      ResizeIfNeed(8);
      WriteLong(value, _offset);
      _offset += 8;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int WriteLong(long value, int offset)
    {
      var converter = new ValueConverter() { Long = value };
      _data[offset] = converter.Byte0;
      _data[offset + 1] = converter.Byte1;
      _data[offset + 2] = converter.Byte2;
      _data[offset + 3] = converter.Byte3;
      _data[offset + 4] = converter.Byte4;
      _data[offset + 5] = converter.Byte5;
      _data[offset + 6] = converter.Byte6;
      _data[offset + 7] = converter.Byte7;
      return 8;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long ReadLong()
    {
      var converter = new ValueConverter
      {
        Byte0 = _data[_offset],
        Byte1 = _data[_offset + 1],
        Byte2 = _data[_offset + 2],
        Byte3 = _data[_offset + 3],
        Byte4 = _data[_offset + 4],
        Byte5 = _data[_offset + 5],
        Byte6 = _data[_offset + 6],
        Byte7 = _data[_offset + 7],
      };
      _offset += 8;
      return converter.Long;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int WriteFloat(float value)
    {
      var converter = new ValueConverter() { Float = value };
      WriteInt(converter.Int);
      return 4;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float ReadFloat()
    {
      var rawValue = ReadInt();
      var converter = new ValueConverter() { Int = rawValue };
      var value = converter.Float;
      return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int WriteString(string value)
    {
      var rawData = Encoding.UTF8.GetBytes(value);
      var len = rawData.Length;
      ResizeIfNeed(2 + len);
      WriteUShort((ushort)len);

      Buffer.BlockCopy(rawData, 0, _data, _offset, len);
      
      _offset += len;

      return len + 2;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ReadString()
    {
      var len = ReadUShort();
      var rawData = new byte[len];

      Buffer.BlockCopy(_data, _offset, rawData, 0, len);

      var str = Encoding.UTF8.GetString(rawData);
      _offset += len;
      return str;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte[] ReadBinary(int len)
    {
      if (len >= _data.Length)
        return Array.Empty<byte>();

      var payload = new byte[len];
      Buffer.BlockCopy(_data, _offset, payload, 0, len);

      _offset += len;

      return payload;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteBinary(byte[] payload)
    {
      ResizeIfNeed(payload.Length);

      Array.Copy(payload, 0, _data, _offset, payload.Length);

      _offset += payload.Length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteOperation(RagonOperation operation)
    {
      ResizeIfNeed(1);

      _data[_offset] = (byte)operation;
      _offset += 1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public RagonOperation ReadOperation()
    {
      var op = (RagonOperation)_data[_offset];
      _offset += 1;
      return op;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteUShort(ushort value)
    {
      ResizeIfNeed(2);

      _data[_offset] = (byte)(value & 0x00FF);
      _data[_offset + 1] = (byte)((value & 0xFF00) >> 8);
      _offset += 2;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteUShort(ushort value, int offset)
    {
      ResizeIfNeed(2);
      _data[offset] = (byte)(value & 0x00FF);
      _data[offset + 1] = (byte)((value & 0xFF00) >> 8);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort ReadUShort()
    {
      var value = (ushort)(_data[_offset] + (_data[_offset + 1] << 8));
      _offset += 2;
      return value;
    }

    public void Clear()
    {
      _offset = 0;
      _size = 0;
    }


    public void FromArray(byte[] data)
    {
      Clear();
      ResizeIfNeed(data.Length);
      Buffer.BlockCopy(data, 0, _data, 0, data.Length);
      _size = data.Length;
    }


    public byte[] ToArray()
    {
      var bytes = new byte[_offset];
      Buffer.BlockCopy(_data, 0, bytes, 0, _offset);
      return bytes;
    }

    private void ResizeIfNeed(int lenght)
    {
      if (_offset + lenght < _data.Length)
        return;

      var newData = new byte[_data.Length * 4 + lenght];
      Buffer.BlockCopy(_data, 0, newData, 0, _data.Length);
      _data = newData;
    }
  }
}