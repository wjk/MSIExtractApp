// Copyright (c) William Kent. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace MSIExtract.Controls
{
    /// <summary>
    /// Contains WPF command definitions, and utility methods for working with commands.
    /// </summary>
    public static class Commands
    {
        /// <summary>
        /// Creates a <see cref="RoutedCommand"/> with the given name, owner type, and gesture list.
        /// </summary>
        /// <param name="name">
        /// The name of the command.
        /// </param>
        /// <param name="ownerType">
        /// The type that owns the command.
        /// </param>
        /// <param name="gestures">
        /// A set of input gestures that trigger the command.
        /// </param>
        /// <returns>
        /// A <see cref="RoutedCommand"/> instance.
        /// </returns>
        public static RoutedCommand CreateCommand(string name, Type ownerType, params InputGesture[] gestures)
        {
            return new RoutedCommand(name, ownerType, new InputGestureCollection(gestures));
        }

        /// <summary>
        /// Creates a <see cref="RoutedUICommand"/> with the given name, owner type, text, and gesture list.
        /// </summary>
        /// <param name="name">
        /// The name of the command.
        /// </param>
        /// <param name="ownerType">
        /// The type that owns the command.
        /// </param>
        /// <param name="text">
        /// The text the command should display.
        /// </param>
        /// <param name="gestures">
        /// A set of input gestures that trigger the command.
        /// </param>
        /// <returns>
        /// A <see cref="RoutedUICommand"/> instance.
        /// </returns>
        public static RoutedUICommand CreateUICommand(string name, Type ownerType, string text, params InputGesture[] gestures)
        {
            return new RoutedUICommand(text, name, ownerType, new InputGestureCollection(gestures));
        }
    }
}
