// Copyright (c) William Kent. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

namespace MSIExtract
{
    /// <summary>
    /// The main window for the application.
    /// </summary>
    public partial class MainWindow
    {
        /// <summary>
        /// Identifier for the "Clear Recent Files" command.
        /// </summary>
        public static readonly RoutedCommand ClearRecentFileListCommand = Commands.CreateCommand("ClearRecentFileList", typeof(MainWindow));

        /// <summary>
        /// Identifier for the "Open Recent File" command.
        /// </summary>
        public static readonly RoutedCommand OpenRecentFileCommand = Commands.CreateCommand("OpenRecentFile", typeof(MainWindow));

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindow"/> class.
        /// </summary>
        public MainWindow()
        {
            this.InitializeComponent();
            DataContext = new AppModel();
        }

        private void CloseWindowCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Close();
        }

        private void ClearRecentFileListCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var model = (AppModel)DataContext;
            model.ClearMRU();
        }

        private void OpenRecentFileCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var entry = (MRULib.MRU.Interfaces.IMRUEntryViewModel)e.Parameter;
            var model = (AppModel)DataContext;

            model.MsiPath = entry.PathFileName;
        }
    }
}
