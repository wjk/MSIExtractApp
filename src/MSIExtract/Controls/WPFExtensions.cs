// Copyright (c) William Kent. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Windows;
using System.Windows.Interop;

using FolderBrowserDialog = System.Windows.Forms.FolderBrowserDialog;
using IWin32Window = System.Windows.Forms.IWin32Window;

namespace MSIExtract.Controls
{
    /// <summary>
    /// Provides extension methods to assist with WPF/Windows Forms interop.
    /// </summary>
    public static class WPFExtensions
    {
        /// <summary>
        /// Shows a <see cref="FolderBrowserDialog"/> modal to a WPF <see cref="Window"/>.
        /// </summary>
        /// <param name="dialog">
        /// The <see cref="FolderBrowserDialog"/> to show.
        /// </param>
        /// <param name="window">
        /// The <see cref="Window"/> to use as the modal parent.
        /// </param>
        /// <returns>
        /// <c>true</c> if the OK button was clicked; <c>false</c> otherwise.
        /// </returns>
        public static bool ShowDialog(this FolderBrowserDialog dialog, Window window)
        {
            if (dialog == null)
            {
                throw new ArgumentNullException(nameof(dialog));
            }

            if (window == null)
            {
                throw new ArgumentNullException(nameof(window));
            }

            var owner = new Win32Window(new WindowInteropHelper(window).Handle);
            return dialog.ShowDialog(owner) == System.Windows.Forms.DialogResult.OK;
        }

        private class Win32Window : IWin32Window
        {
            public Win32Window(IntPtr handle)
            {
                Handle = handle;
            }

            public IntPtr Handle { get; }
        }
    }
}
