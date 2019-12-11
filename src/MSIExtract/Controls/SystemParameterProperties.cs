// Copyright (c) William Kent. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Windows;

namespace MSIExtract.Controls
{
    /// <summary>
    /// Provides attached dependency properties that front for
    /// values obtained from <see cref="SystemParameters"/>.
    /// </summary>
    public static class SystemParameterProperties
    {
        /// <summary>
        /// Provides identity for the <c>SystemParameterProperties.HighContrast</c> XAML property.
        /// </summary>
        public static readonly DependencyProperty HighContrastProperty =
        DependencyProperty.RegisterAttached("HighContrast", typeof(bool), typeof(SystemParameterProperties), new FrameworkPropertyMetadata() { Inherits = true });

        /// <summary>
        /// Gets the value of the <see cref="HighContrastProperty"/>.
        /// </summary>
        /// <param name="target">
        /// The <see cref="DependencyObject"/> to get the value from.
        /// </param>
        /// <returns>
        /// A <see cref="bool"/> value.
        /// </returns>
        public static bool GetHighContrast(DependencyObject target)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            return (bool)target.GetValue(HighContrastProperty);
        }

        /// <summary>
        /// Sets the value of the <see cref="HighContrastProperty"/>.
        /// </summary>
        /// <param name="target">
        /// The <see cref="DependencyObject"/> to set the value on.
        /// </param>
        /// <param name="value">
        /// Whether or not High Contrast mode is enabled.
        /// </param>
        public static void SetHighContrast(DependencyObject target, bool value)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            target.SetValue(HighContrastProperty, value);
        }
    }
}
