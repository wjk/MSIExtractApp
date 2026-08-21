// Copyright (c) William Kent. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
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
using MSIExtract.Controls.Localization;

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

        private readonly PRIResourceLoader stringLoader = new PRIResourceLoader(typeof(MainWindow), nameof(MainWindow));

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindow"/> class.
        /// </summary>
        public MainWindow()
        {
            this.InitializeComponent();
            DataContext = new AppModel();

            // A LocalizeExtension in the top-level element causes an exception because
            // it is evaluated before our Resources directionary is created, thus giving
            // it no way to locate its PRI file.
            Title = stringLoader.GetString("Window.Title");
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

            void ShowFileMissingDialog(string path)
            {
                TaskDialogPage page = new TaskDialogPage
                {
                    AllowCancel = true,
                    Title = stringLoader.GetString("Window.Title"),
                    Instruction = string.Format(stringLoader.GetString("FileNotFoundDialog.Instruction"), path),
                    Text = stringLoader.GetString("FileNotFoundDialog.Text"),
                    Icon = TaskDialogIcon.Get(TaskDialogStandardIcon.Warning),
                };

                TaskDialogCustomButton removeButton = new TaskDialogCustomButton(stringLoader.GetString("FileNotFoundDialog.RemoveButtonText"));
                removeButton.DefaultButton = true;
                page.CustomButtons.Add(removeButton);
                page.StandardButtons.Add(TaskDialogResult.Cancel);

                TaskDialog dialog = new TaskDialog(page);
                if (dialog.Show(this).Equals(removeButton))
                {
                    model.RemoveMRUItem(entry);
                }
            }

            if (!File.Exists(entry.PathFileName))
            {
                string fileName = System.IO.Path.GetFileName(entry.PathFileName);
                ShowFileMissingDialog(fileName);
                return;
            }

            try
            {
                model.MsiPath = entry.PathFileName;
            }
            catch (WixToolset.Dtf.WindowsInstaller.InstallerException)
            {
                ShowFileMissingDialog(entry.File.Name);
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
                string appTitle = ThisAssembly.AssemblyTitle;
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
                Title = stringLoader.GetString("AboutDialog.Title"),
                Instruction = GetTaskDialogInstruction(),
                Icon = TaskDialogIcon.Get(TaskDialogStandardIcon.Information),
                Text = string.Format(stringLoader.GetString("AboutDialog.Text"), ThisAssembly.AssemblyInformationalVersion),
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

            TaskDialog.Show(this, page);
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
            page.Title = stringLoader.GetString("Window.Title");
            page.Instruction = string.Format(stringLoader.GetString("InvalidFileDialog.Instruction"), fileName);
            page.Text = stringLoader.GetString("InvalidFileDialog.Text");
            page.Icon = TaskDialogStandardIcon.Error;
            page.StandardButtons.Add(TaskDialogResult.OK);

            TaskDialog.Show(this, page);
        }
    }
}
