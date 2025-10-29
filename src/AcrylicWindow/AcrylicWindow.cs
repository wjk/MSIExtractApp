// Copyright (c) William Kent. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Dwm;

namespace AcrylicWindow;

/// <summary>
/// A WPF <see cref="Window"/> that supports displaying Windows 11 theme
/// backgrounds on the non-client area.
/// </summary>
public class AcrylicWindow : Window
{
#pragma warning disable SA1117 // Parameters should be on same line or separate lines (wart)
    /// <summary>
    /// Provides the identity for the <see cref="FrameBackground"/> property.
    /// </summary>
    public static readonly DependencyProperty FrameBackgroundProperty =
        DependencyProperty.Register(nameof(FrameBackground), typeof(WindowFrameBackground), typeof(AcrylicWindow),
            new FrameworkPropertyMetadata(WindowFrameBackground.Solid, FrameBackgroundChanged));

    /// <summary>
    /// Provides the identity for the <see cref="EnableFrameDarkMode"/> property.
    /// </summary>
    public static readonly DependencyProperty EnableFrameDarkModeProperty =
        DependencyProperty.Register(nameof(EnableFrameDarkMode), typeof(bool), typeof(AcrylicWindow),
            new FrameworkPropertyMetadata(false, FrameDarkModeChanged));
#pragma warning restore SA1117 // Parameters should be on same line or separate lines

    /// <summary>
    /// Gets or sets the kind of background the window frame will have.
    /// </summary>
    public WindowFrameBackground FrameBackground
    {
        get => (WindowFrameBackground)this.GetValue(FrameBackgroundProperty);
        set => this.SetValue(FrameBackgroundProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether or not the system should modify the window frame
    /// background when Dark Mode is enabled.
    /// </summary>
    public bool EnableFrameDarkMode
    {
        get => (bool)this.GetValue(EnableFrameDarkModeProperty);
        set => this.SetValue(EnableFrameDarkModeProperty, value);
    }

    private static void FrameBackgroundChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
    {
        if (e.Property.Name != nameof(FrameBackground))
        {
            throw new InvalidOperationException("Unexpected WPF property name");
        }

        AcrylicWindow window = (AcrylicWindow)target;
        WindowInteropHelper helper = new WindowInteropHelper(window);
        helper.EnsureHandle();

        DWM_SYSTEMBACKDROP_TYPE backdropType = (WindowFrameBackground)e.NewValue switch
        {
            WindowFrameBackground.Solid => DWM_SYSTEMBACKDROP_TYPE.DWMSBT_NONE,
            WindowFrameBackground.MainWindow => DWM_SYSTEMBACKDROP_TYPE.DWMSBT_MAINWINDOW,
            WindowFrameBackground.SupportingWindow => DWM_SYSTEMBACKDROP_TYPE.DWMSBT_TRANSIENTWINDOW,
            _ => throw new ArgumentException("Unexpected type", "e.NewValue")
        };

        unsafe
        {
            PInvoke.DwmSetWindowAttribute(
                new HWND(helper.Handle),
                DWMWINDOWATTRIBUTE.DWMWA_SYSTEMBACKDROP_TYPE,
                &backdropType,
                sizeof(DWM_SYSTEMBACKDROP_TYPE));
        }
    }

    private static void FrameDarkModeChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
    {
        if (e.Property.Name != nameof(EnableFrameDarkMode))
        {
            throw new InvalidOperationException("Unexpected WPF property name");
        }

        AcrylicWindow window = (AcrylicWindow)target;
        WindowInteropHelper helper = new WindowInteropHelper(window);
        helper.EnsureHandle();

        unsafe
        {
            BOOL value = (bool)e.NewValue;
            PInvoke.DwmSetWindowAttribute(
                new HWND(helper.Handle),
                DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE,
                &value,
                Convert.ToUInt32(sizeof(BOOL)));
        }
    }
}
