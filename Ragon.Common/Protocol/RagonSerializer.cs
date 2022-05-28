using System;
using System.Runtime.CompilerServices;
using System.Text;


namespace Ragon.Common
{

  public class RagonSerializer
  {
    private byte[] _data;
    private int _offset;
    private int _size;
    public int Lenght => _offset;
    public int Size => _size - _offset;
    
    public RagonSerializer(int capacity = 2048)
    {
      _data = new byte[capacity];
      _offset = 0;
      _size = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteByte(byte value)
    {
      ResizeIfNeed(1);
      
      _data[_offset] = value;
      _offset += 1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int ReadByte()
    {
      var value = _data[_offset];
      _offset += 1;
      return value;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteInt(int value)
    {
      ResizeIfNeed(4);
      
      _data[_offset] = (byte) (value & 0x00FF);
      _data[_offset + 1] = (byte) ((value & 0xFF00) >> 8);
      _data[_offset + 2] = (byte) ((value & 0xFF00) >> 16);
      _data[_offset + 3] = (byte) ((value & 0xFF00) >> 24);
      _offset += 4;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int ReadInt()
    {
      var value = _data[_offset] + (_data[_offset + 1] << 8) + (_data[_offset + 2] << 16) + (_data[_offset + 3] << 24);
      _offset += 4;
      return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteString(string value)
    {
      var stringRaw = Encoding.UTF8.GetBytes(value).AsSpan();
      ResizeIfNeed(2 + stringRaw.Length);
      
      WriteUShort((ushort) stringRaw.Length);
      var data = _data.AsSpan().Slice(_offset, stringRaw.Length);
      stringRaw.CopyTo(data);
      _offset += stringRaw.Length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ReadString()
    {
      var lenght = ReadUShort();
      var stringRaw = _data.AsSpan().Slice(_offset, lenght);
      var str = Encoding.UTF8.GetString(stringRaw);
      _offset += lenght;
      return str;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<byte> ReadData(int lenght)
    {
      var data = _data.AsSpan();
      var payloadData = data.Slice(_offset, lenght);
      
      _offset += payloadData.Length;
      return payloadData;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteData(ref ReadOnlySpan<byte> payload)
    {
      ResizeIfNeed(payload.Length);
      
      var data = _data.AsSpan();
      var payloadData = data.Slice(_offset, payload.Length);

      payload.CopyTo(payloadData);
      _offset += payload.Length;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<byte> GetWritableData(int lenght)
    {
      ResizeIfNeed(lenght);
      
      var data = _data.AsSpan();
      var payloadData = data.Slice(_offset, lenght);

      _offset += lenght;
      return payloadData;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteOperation(RagonOperation ragonOperation)
    {
      ResizeIfNeed(1);
      
      _data[_offset] = (byte) ragonOperation;
      _offset += 1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public RagonOperation ReadOperation()
    {
      var op = (RagonOperation) _data[_offset];
      _offset += 1;
      return op;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteUShort(ushort value)
    {
      ResizeIfNeed(2);
      
      _data[_offset] = (byte) (value & 0x00FF);
      _data[_offset + 1] = (byte) ((value & 0xFF00) >> 8);
      _offset += 2;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort ReadUShort()
    {
      var value = (ushort) (_data[_offset] + (_data[_offset + 1] << 8));
      _offset += 2;
      return value;
    }

    public void Clear()
    {
      _offset = 0;
      _size = 0;
    }

    public void ToSpan(ref Span<byte> data)
    {
      var span = _data.AsSpan();
      var dataSpan = span.Slice(0, _offset);
      dataSpan.CopyTo(data);
    }

    public void FromSpan(ref ReadOnlySpan<byte> data)
    {
      Clear();
      ResizeIfNeed(data.Length);
      var dataSpan = _data.AsSpan();
      data.CopyTo(dataSpan);
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
      
      var newData = new byte[_data.Length * 2];
      Buffer.BlockCopy(_data, 0, newData, 0, _data.Length);
      _data = newData;
    }
  }
}