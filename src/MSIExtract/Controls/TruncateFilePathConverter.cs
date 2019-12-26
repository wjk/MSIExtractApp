// Copyright (c) William Kent. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;

namespace MSIExtract.Controls
{
    /// <summary>
    /// Converts a file path into a shorter file path, possibly with some elements elided.
    /// </summary>
    public class TruncateFilePathConverter : IValueConverter
    {
        private const int MaxLength = 50;

        /// <summary>
        /// Converts a value to another type.
        /// </summary>
        /// <param name="value">
        /// The value to convert.
        /// </param>
        /// <param name="targetType">
        /// The type to convert to.
        /// </param>
        /// <param name="parameter">
        /// An optional parameter.
        /// </param>
        /// <param name="culture">
        /// The UI culture to convert to.
        /// </param>
        /// <returns>
        /// The converted value.
        /// </returns>
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var path = value?.ToString();
            if (path == null)
            {
                return null;
            }
            else if (path.Length <= MaxLength)
            {
                return path;
            }
            else
            {
                return Truncate(path);
            }
        }

        /// <summary>
        /// Not implemented. Always throws <see cref="NotImplementedException"/>.
        /// </summary>
        /// <param name="value">
        /// The value to convert.
        /// </param>
        /// <param name="targetType">
        /// The type to convert to.
        /// </param>
        /// <param name="parameter">
        /// An optional parameter.
        /// </param>
        /// <param name="culture">
        /// The UI culture to convert to.
        /// </param>
        /// <returns>
        /// The converted value.
        /// </returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        private static string Truncate(string path)
        {
            char separator;
            var prefix = string.Empty;

            if (Path.IsPathRooted(path))
            {
                separator = Path.DirectorySeparatorChar;
                var index = path.IndexOf(Path.VolumeSeparatorChar, StringComparison.Ordinal);
                if (index > -1)
                {
                    prefix = path.Substring(0, Math.Min(path.Length, index + 2));
                    path = path.Substring(index + 2);
                }
            }
            else
            {
                separator = '/';
                var index = path.IndexOf(Uri.SchemeDelimiter, StringComparison.Ordinal);
                if (index > -1)
                {
                    prefix = path.Substring(0, Math.Min(path.Length, index + 3));
                    path = path.Substring(index + 3);
                }
            }

            var parts = path.Split(new[] { separator }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1)
            {
                return parts[0];
            }

            var remainingLength = MaxLength - prefix.Length - 3; // 3 is the length of '...'
            var res = string.Empty;
            for (var i = parts.Length - 1; i >= 0; --i)
            {
                if (res.Length + parts[i].Length + 1 <= remainingLength)
                {
                    res = separator + parts[i] + res;
                }
                else
                {
                    break;
                }
            }

            if (res.Length == 0 && parts.Length > 0)
            {
                var lastPart = parts[^1];
                res = lastPart.Substring(Math.Max(0, lastPart.Length - remainingLength));
            }

            return prefix + "..." + res;
        }
    }
}
