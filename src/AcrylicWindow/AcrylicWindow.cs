// Copyright (c) William Kent. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

using Windows.UI.ViewManagement;
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
    /// <summary>
    /// Provides the identity for the <see cref="FrameBackground"/> property.
    /// </summary>
    [SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1117:Parameters should be on same line or separate lines", Justification = "wart")]
    public static readonly DependencyProperty FrameBackgroundProperty =
        DependencyProperty.Register(nameof(FrameBackground), typeof(WindowFrameBackground), typeof(AcrylicWindow),
            new FrameworkPropertyMetadata(WindowFrameBackground.Solid, FrameBackgroundChanged));

    private UISettings themeSettings = new UISettings();
    private bool hookedThemeChanged = false;

    /// <summary>
    /// Gets or sets the kind of background the window frame will have.
    /// </summary>
    public WindowFrameBackground FrameBackground
    {
        get => (WindowFrameBackground)this.GetValue(FrameBackgroundProperty);
        set => this.SetValue(FrameBackgroundProperty, value);
    }

    /// <inheritdoc/>
    protected override void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);

        if (!this.hookedThemeChanged)
        {
            this.themeSettings.ColorValuesChanged += this.ThemeSettings_ColorValuesChanged;
            this.hookedThemeChanged = true;
        }
    }

    private static void FrameBackgroundChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
    {
        if (e.Property.Name != nameof(FrameBackground))
        {
            throw new InvalidOperationException("Unexpected WPF property name");
        }

        var window = (AcrylicWindow)target;
        window.SetDwmAttribute();
    }

    private void ThemeSettings_ColorValuesChanged(UISettings sender, object args)
    {
        // This method is called on a background thread. If we try to access
        // the window's handle from a background thread, an exception occurs.
        this.Dispatcher.BeginInvoke(this.SetDwmAttribute);
    }

    private void SetDwmAttribute()
    {
        if (Environment.OSVersion.Version.Build < 22621)
        {
            // Windows version not new enough to support this feature, bail.
            return;
        }

        var helper = new WindowInteropHelper(this);
        helper.EnsureHandle();

        DWM_SYSTEMBACKDROP_TYPE backdropType = this.FrameBackground switch
        {
            WindowFrameBackground.Solid => DWM_SYSTEMBACKDROP_TYPE.DWMSBT_NONE,
            WindowFrameBackground.MainWindow => DWM_SYSTEMBACKDROP_TYPE.DWMSBT_MAINWINDOW,
            WindowFrameBackground.SupportingWindow => DWM_SYSTEMBACKDROP_TYPE.DWMSBT_TRANSIENTWINDOW,
            _ => throw new ArgumentException("Unexpected FrameBackground value")
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
}
