// Copyright (c) William Kent. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using Vanara.PInvoke;

namespace MSIExtract.Controls
{
    /// <summary>
    /// Converts absolute (rooted) file paths to strings suitable for display in the UI.
    /// </summary>
    public sealed class FileNameConverter : IValueConverter
    {
        /// <summary>
        /// Gets or sets a value of type <see cref="FileNameDisplayMode"/> that indicates what kind of string to convert to.
        /// </summary>
        public FileNameDisplayMode DisplayMode { get; set; } = FileNameDisplayMode.Default;

        /// <summary>
        /// Gets or sets the string to use when the input path is <c>null</c> or <see cref="string.Empty"/>.
        /// </summary>
        public string FallbackValue { get; set; } = string.Empty;

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
        public object? Convert(object? value, Type targetType, object parameter, CultureInfo culture)
        {
            if (targetType != typeof(string))
            {
                throw new InvalidOperationException("Cannot convert to any type except System.String");
            }

            if (value == null)
            {
                return FallbackValue;
            }

            var path = (string)value;
            if (!Path.IsPathRooted(path))
            {
                throw new ArgumentException("Path must be rooted", nameof(value));
            }

            Shell32.IShellItem2 item = Shell32.SHCreateItemFromParsingName<Shell32.IShellItem2>(path);

            Shell32.SIGDN sigdn = DisplayMode switch
            {
                FileNameDisplayMode.Default => Shell32.SIGDN.SIGDN_NORMALDISPLAY,
                FileNameDisplayMode.FullPath => Shell32.SIGDN.SIGDN_FILESYSPATH,
                FileNameDisplayMode.NameOnly => Shell32.SIGDN.SIGDN_PARENTRELATIVEPARSING,
                _ => throw new InvalidOperationException($"Unexpected {nameof(FileNameDisplayMode)}")
            };

            return item.GetDisplayName(sigdn);
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
    }
}
