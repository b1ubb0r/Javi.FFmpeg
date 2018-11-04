using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace MediaToolkit.Util
{
    public static class Extensions
    {
        private const int BUFF_SIZE = 16*1024;

        internal static void CopyTo(this Stream input, Stream output)
        {
            byte[] buffer = new byte[Extensions.BUFF_SIZE];
            int bytesRead;

            while ((bytesRead = input.Read(buffer, 0, buffer.Length)) > 0) { output.Write(buffer, 0, bytesRead); }
        }

        public static string FormatInvariant(this string value, params object[] args)
        {
            try
            {
                return value == null
                    ? string.Empty
                    : string.Format(CultureInfo.InvariantCulture, value, args);
            }
            catch (FormatException) {
                return value;
            }
        }

        internal static string Remove(this Enum enumerable, string text)
        {
            return enumerable.ToString()
                .Replace(text, "");
        }

        internal static string ToLower(this Enum enumerable)
        {
            return enumerable.ToString()
                .ToLowerInvariant();
        }

    }
}