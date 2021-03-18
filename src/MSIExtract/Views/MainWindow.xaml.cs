// Copyright (c) William Kent. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
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
using KPreisser.UI;
using MSIExtract.Controls;
using Vanara.PInvoke;

namespace MSIExtract.Views
{
    /// <summary>
    /// The main window for the application.
    /// </summary>
    public partial class MainWindow
    {
        /// <summary>
        /// Identifier for a command that displays an error message stating the MSI/MSM file is invalid.
        /// </summary>
        public static readonly RoutedCommand ShowInvalidFileErrorCommand = Commands.CreateCommand("ShowInvalidFileError", typeof(MainWindow));

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

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindow"/> class, and opens a file immediately.
        /// </summary>
        /// <param name="msiPath">
        /// A path to an MSI file to open immediately.
        /// </param>
        public MainWindow(string msiPath)
            : this()
        {
            AppModel model = (AppModel)DataContext;
            model.MsiPath = msiPath;
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

            try
            {
                model.MsiPath = entry.PathFileName;
            }
            catch (WixToolset.Dtf.WindowsInstaller.InstallerException)
            {
                TaskDialogPage page = new TaskDialogPage
                {
                    AllowCancel = true,
                    Title = "MSI Viewer",
                    Instruction = $"The file {entry.File.Name} does not exist. Would you like to remove it from the Recent Files list?",
                    Icon = TaskDialogIcon.Get(TaskDialogStandardIcon.Warning),
                };

                TaskDialogCustomButton removeButton = new TaskDialogCustomButton("Remove");
                removeButton.DefaultButton = true;
                page.CustomButtons.Add(removeButton);
                page.StandardButtons.Add(TaskDialogResult.Cancel);

                TaskDialog dialog = new TaskDialog(page);
                if (dialog.Show(this).Equals(removeButton))
                {
                    model.RemoveMRUItem(entry);
                }
            }
        }

        private void OpenCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            FilePicker.ShowChooseFileDialog();
        }

        [SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1117:Parameters should be on same line or separate lines", Justification = "wart")]
        private void AboutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            static string GetTaskDialogInstruction()
            {
                string appTitle = "MSI Viewer";
                string versionString = ThisAssembly.AssemblyVersion;

                if (versionString.EndsWith(".0.0", StringComparison.InvariantCulture))
                {
                    versionString = versionString.Substring(0, versionString.Length - 4);
                }
                else if (versionString.EndsWith(".0", StringComparison.InvariantCulture))
                {
                    versionString = versionString.Substring(0, versionString.Length - 2);
                }

                return appTitle + " " + versionString;
            }

            TaskDialogPage page = new TaskDialogPage
            {
                AllowCancel = true,
                Title = "About MSI Viewer",
                Instruction = GetTaskDialogInstruction(),
                Icon = TaskDialogIcon.Get(TaskDialogStandardIcon.Information),
                Text = "Copyright © 2019-2020 William Kent. Licensed under the MIT License.\r\n\r\n" +
                "<a href=\"github\">View on GitHub</a>\r\n" +
                "<a href=\"tpn\">Third-Party Notices</a>\r\n\r\n" +
                $"Build {ThisAssembly.AssemblyInformationalVersion}",
                EnableHyperlinks = true,
            };
            page.StandardButtons.Add(TaskDialogResult.OK);

            page.HyperlinkClicked += (s, e) =>
            {
                if (e.Hyperlink == "github")
                {
                    Shell32.ShellExecute(IntPtr.Zero, "open", "https://github.com/wjk/MSIExtractApp",
                       null, null, ShowWindowCommand.SW_SHOWDEFAULT);
                }
                else if (e.Hyperlink == "tpn")
                {
                    Shell32.ShellExecute(IntPtr.Zero, "open", "https://github.com/wjk/MSIExtractApp/blob/main/legal/ThirdPartyNotices.md",
                        null, null, ShowWindowCommand.SW_SHOWDEFAULT);
                }
            };

            TaskDialog dialog = new TaskDialog(page);
            dialog.Show(this);
        }

        [SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1117:Parameters should be on same line or separate lines", Justification = "wart")]
        private void PrivacyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Shell32.ShellExecute(IntPtr.Zero, "open", "https://github.com/wjk/MSIExtractApp/blob/main/legal/PrivacyPolicy.md",
                null, null, ShowWindowCommand.SW_SHOWDEFAULT);
        }

        private void ShowInvalidFileCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            string fileName = System.IO.Path.GetFileName((string)e.Parameter);

            TaskDialogPage page = new TaskDialogPage();
            page.AllowCancel = true;
            page.Title = "MSI Viewer";
            page.Instruction = $"Could not open \"{fileName}\".";
            page.Text = "This file may not be a valid MSI or MSM file.";
            page.Icon = TaskDialogStandardIcon.Error;
            page.StandardButtons.Add(TaskDialogResult.OK);

            TaskDialog dialog = new TaskDialog(page);
            dialog.StartupLocation = TaskDialogStartupLocation.CenterParent;
            dialog.Show(this);
        }
    }
}
