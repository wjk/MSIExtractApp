// Copyright (c) William Kent. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MSIExtract.Controls;

namespace MSIExtract.Views
{
    /// <summary>
    /// Interaction logic for the "Extract Files" control.
    /// </summary>
    public partial class ExtractFilesView : UserControl
    {
        /// <summary>
        /// Identifier for the "Select None" command.
        /// </summary>
        public static readonly RoutedCommand SelectNoneCommand = Commands.CreateCommand("SelectNone", typeof(ExtractFilesView), new KeyGesture(Key.A, ModifierKeys.Control | ModifierKeys.Shift));

        /// <summary>
        /// Initializes a new instance of the <see cref="ExtractFilesView"/> class.
        /// </summary>
        public ExtractFilesView()
        {
            InitializeComponent();
        }

        private void SelectAllCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            FileListView.SelectAll();
        }

        private void SelectNoneCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            FileListView.SelectedItems.Clear();
        }
    }
}
