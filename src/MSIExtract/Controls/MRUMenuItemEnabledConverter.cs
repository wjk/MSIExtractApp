// Copyright (c) William Kent. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using System.Windows.Data;
using MRULib.MRU.Models.Persist;

namespace MSIExtract.Controls
{
    /// <summary>
    /// Converts a <see cref="MRUList"/> object into a <see cref="bool"/> value
    /// that is used to determine whether or not the "Recent Files" menu item is enabled.
    /// </summary>
    public sealed class MRUMenuItemEnabledConverter : IValueConverter
    {
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
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (targetType != typeof(bool))
            {
                throw new ArgumentException("Cannot convert to any type other than System.Boolean", nameof(targetType));
            }

            if (value == null)
            {
                // If there is no MRU list, then there is nothing to display.
                return false;
            }

            MRUList list = (MRUList)value;
            return list.ListOfMRUEntries.Count > 0;
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
