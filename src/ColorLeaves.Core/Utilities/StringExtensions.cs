using System;

namespace ColorLeaves.Core.Utilities;

public static class StringExtensions
{
    public static bool IsNullOrEmpty(this string value) => string.IsNullOrEmpty(value);
    public static bool IsNotNullOrEmpty(this string value) => !string.IsNullOrEmpty(value);
}
