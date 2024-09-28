using Ragon.Client.Compressor;
using Ragon.Protocol;

namespace Ragon.Client.Utils;

public static class CompressorExtension
{
  public static float Read(this FloatCompressor compressor, RagonBuffer buffer)
  {
    return compressor.Decompress(buffer.Read(compressor.RequiredBits));
  }

  public static void Write(this FloatCompressor compressor, RagonBuffer buffer, float value)
  {
    buffer.Write(compressor.Compress(value), compressor.RequiredBits);
  }
  
  public static float Read(this IntCompressor compressor, RagonBuffer buffer)
  {
    return compressor.Decompress(buffer.Read(compressor.RequiredBits));
  }

  public static void Write(this IntCompressor compressor, RagonBuffer buffer, int value)
  {
    buffer.Write(compressor.Compress(value), compressor.RequiredBits);
  }
}