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
        /// Provides identity for the <see cref="FilePath"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty FilePathProperty = DependencyProperty.Register(
            nameof(FilePath),
            typeof(string),
            typeof(FilePicker),
            new FrameworkPropertyMetadata(
                propertyChangedCallback: (obj, args) => ((FilePicker)obj).FilePropertyChanged((string)args.NewValue),
                coerceValueCallback: (obj, val) => CoerceFile(val)));

        /// <summary>
        /// Provides identity for the <see cref="IsReadOnly"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsReadOnlyProperty = DependencyProperty.Register(nameof(IsReadOnly), typeof(bool), typeof(FilePicker), new FrameworkPropertyMetadata(false));

        /// <summary>
        /// Provides identity for the <see cref="OpenDialogFilter"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty OpenDialogFilterProperty = DependencyProperty.Register(nameof(OpenDialogFilter), typeof(string), typeof(FilePicker), new PropertyMetadata(null));

        /// <summary>
        /// Provides identity for the <see cref="Header"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register(nameof(Header), typeof(object), typeof(FilePicker), new PropertyMetadata(null));

        private Image? iconPart;
        private Button? chooseButtonPart;

        static FilePicker()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(FilePicker), new FrameworkPropertyMetadata(typeof(FilePicker)));
        }

        /// <summary>
        /// Gets or sets the selected file.
        /// </summary>
        public string? FilePath
        {
            get => (string?)GetValue(FilePathProperty);
            set => SetValue(FilePathProperty, value);
        }

        /// <summary>
        /// Gets or sets the filter string used in the choose-file dialog.
        /// </summary>
        public string? OpenDialogFilter
        {
            get => (string?)GetValue(OpenDialogFilterProperty);
            set => SetValue(OpenDialogFilterProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the user can change the selection.
        /// </summary>
        public bool IsReadOnly
        {
            get => (bool)GetValue(IsReadOnlyProperty);
            set => SetValue(IsReadOnlyProperty, value);
        }

        /// <summary>
        /// Gets or sets the header (label) content.
        /// </summary>
        public object? Header
        {
            get => GetValue(HeaderProperty);
            set => SetValue(HeaderProperty, value);
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

            FilePropertyChanged(FilePath);
        }

        private static string? CoerceFile(object value)
        {
            if (value == null)
            {
                return null;
            }
            else if (value is string path)
            {
                return path;
            }
            else
            {
                throw new InvalidCastException($"Cannot coerce value of type {value.GetType().FullName} to type System.String");
            }
        }

        private void IconPart_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            FilePropertyChanged(FilePath);
        }

        private void ChooseButtonPart_Click(object sender, RoutedEventArgs e)
        {
            string directory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            if (!string.IsNullOrEmpty(FilePath))
            {
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type (checked immediately below)
                directory = Path.GetDirectoryName(FilePath);
#pragma warning restore CS8600

                if (directory == null)
                {
                    throw new InvalidOperationException("Path.GetDirectoryName() returned null");
                }
            }

            var dialog = new OpenFileDialog
            {
                AddExtension = true,
                Filter = OpenDialogFilter,
                InitialDirectory = directory,
                Title = "Choose File",
                CheckPathExists = true,
                Multiselect = false,
            };

            bool? result = dialog.ShowDialog(Window.GetWindow(this));
            if (result.HasValue && result.Value)
            {
                FilePath = dialog.FileName;
            }
        }

        private void FilePropertyChanged(string? newValue)
        {
            if (iconPart == null)
            {
                // If the icon part wasn't retrieved yet, there's nothing to do here.
                return;
            }

            if (!string.IsNullOrEmpty(newValue))
            {
                if (!Path.IsPathRooted(newValue))
                {
                    throw new ArgumentException("FilePicker.Path must be absolute", nameof(newValue));
                }

                NativeMethods.IShellItem2 shellItem = NativeMethods.SHCreateItemFromParsingName(newValue, IntPtr.Zero, typeof(NativeMethods.IShellItem2).GUID);

                var window = Window.GetWindow(this);
                uint scale = NativeMethods.GetDpiForWindow(new WindowInteropHelper(window).Handle) / 96;
                var iconSize = new NativeMethods.SIZE((int)(iconPart.Width * scale), (int)(iconPart.Height * scale));

                var imageFactory = (NativeMethods.IShellItemImageFactory)shellItem;
                imageFactory.GetImage(iconSize, NativeMethods.SIIGBF.RESIZETOFIT | NativeMethods.SIIGBF.ICONONLY, out IntPtr hBitmap);

                try
                {
                    iconPart.Source = Imaging.CreateBitmapSourceFromHBitmap(hBitmap, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
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
