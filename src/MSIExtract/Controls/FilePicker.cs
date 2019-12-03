// Copyright (c) William Kent. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using MSIExtract.Interop;

namespace MSIExtract.Controls
{
    /// <summary>
    /// Provides a control to choose a file from the filesystem.
    /// </summary>
    [TemplatePart(Name = "PART_Icon", Type = typeof(Image))]
    [TemplatePart(Name = "PART_ChooseButton", Type = typeof(Button))]
    public class FilePicker : Control
    {
        /// <summary>
        /// Provides identity for the <see cref="File"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty FileProperty = DependencyProperty.Register(
            nameof(File),
            typeof(FileInfo),
            typeof(FilePicker),
            new FrameworkPropertyMetadata(
                propertyChangedCallback: (obj, args) => ((FilePicker)obj).FilePropertyChanged((FileInfo)args.NewValue),
                coerceValueCallback: (obj, val) => CoerceFile(val)));

        /// <summary>
        /// Provides identity for the <see cref="IsReadOnly"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsReadOnlyProperty = DependencyProperty.Register(nameof(IsReadOnly), typeof(bool), typeof(FilePicker), new FrameworkPropertyMetadata(false));

        private Image? iconPart;
        private Button? chooseButtonPart;

        static FilePicker()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(FilePicker), new FrameworkPropertyMetadata(typeof(FilePicker)));
        }

        /// <summary>
        /// Gets or sets the selected file.
        /// </summary>
        public FileInfo? File
        {
            get => (FileInfo?)GetValue(FileProperty);
            set => SetValue(FileProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the user can change the selection.
        /// </summary>
        public bool IsReadOnly
        {
            get => (bool)GetValue(IsReadOnlyProperty);
            set => SetValue(IsReadOnlyProperty, value);
        }

        /// <inheritdoc/>
        [SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "Message for non-user-facing exception")]
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (chooseButtonPart != null)
            {
                chooseButtonPart.Click -= ChooseButtonPart_Click;
            }

            if (iconPart != null)
            {
                iconPart.SizeChanged -= IconPart_SizeChanged;
            }

            iconPart = (Image)GetTemplateChild("PART_Icon");
            chooseButtonPart = (Button)GetTemplateChild("PART_ChooseButton");

            if (iconPart == null || chooseButtonPart == null)
            {
                throw new InvalidOperationException("Invalid control template: required template parts not found");
            }

            chooseButtonPart.Click += ChooseButtonPart_Click;
            iconPart.SizeChanged += IconPart_SizeChanged;

            FilePropertyChanged(File);
        }

        private static FileInfo? CoerceFile(object value)
        {
            if (value == null)
            {
                return null;
            }
            else if (value is FileInfo info)
            {
                return info;
            }
            else if (value is string path)
            {
                return new FileInfo(path);
            }
            else
            {
                throw new InvalidCastException($"Cannot coerce value of type {value.GetType().FullName} to type System.IO.FileInfo");
            }
        }

        private void IconPart_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            FilePropertyChanged(File);
        }

        private void ChooseButtonPart_Click(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void FilePropertyChanged(FileInfo? newValue)
        {
            if (iconPart == null)
            {
                // If the icon part wasn't retrieved yet, there's nothing to do here.
                return;
            }

            if (newValue != null)
            {
                NativeMethods.IShellItem2 shellItem = NativeMethods.SHCreateItemFromParsingName(newValue.FullName, IntPtr.Zero, typeof(NativeMethods.IShellItem2).GUID);

                var window = Window.GetWindow(this);
                uint scale = NativeMethods.GetDpiForWindow(new WindowInteropHelper(window).Handle) / 96;
                var iconSize = new NativeMethods.SIZE((int)(iconPart.ActualWidth * scale), (int)(iconPart.ActualHeight * scale));

                var imageFactory = (NativeMethods.IShellItemImageFactory)shellItem;
                imageFactory.GetImage(iconSize, NativeMethods.SIIGBF.RESIZETOFIT | NativeMethods.SIIGBF.ICONONLY, out IntPtr hBitmap);

                try
                {
                    iconPart.Source = Imaging.CreateBitmapSourceFromHBitmap(hBitmap, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                    iconPart.Visibility = Visibility.Visible;
                }
                finally
                {
                    NativeMethods.DeleteObject(hBitmap);
                }
            }
            else
            {
                iconPart.Source = null;
            }
        }
    }
}
