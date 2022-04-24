using System;

namespace Ragon.Core
{
  public static class StringExtensions
  {
    public static string PadBoth(this string str, int length)
    {
      int spaces = length - str.Length;
      int padLeft = spaces / 2 + str.Length;
      return str.PadLeft(padLeft).PadRight(length);
    }
  }
}