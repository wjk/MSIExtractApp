// Copyright (c) William Kent. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using KPreisser.UI;

using Microsoft.Win32;

using MSIExtract.Controls;
using MSIExtract.Controls.Localization;
using MSIExtract.Msi;

using ProgressDialog = Ookii.Dialogs.Wpf.ProgressDialog;

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
        /// Identifier for the "Extract" command.
        /// </summary>
        public static readonly RoutedCommand ExtractCommand = Commands.CreateCommand("Extract", typeof(ExtractFilesView));

        private PRIResourceLoader stringLoader = new PRIResourceLoader(typeof(ExtractFilesView), nameof(ExtractFilesView));

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

        private void SelectionCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = FileListView.Items.Count > 0;
        }

        [SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1130:Use lambda syntax", Justification = "Not valid syntax for some reason")]
        private void ExtractCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (FileListView.SelectedItems.Count == 0)
            {
                // Shouldn't happen (due to ExtractCommand_CanExecute), but check anyway.
                return;
            }

            AppModel model = (AppModel)DataContext;
            OpenFolderDialog browserDialog = new OpenFolderDialog
            {
                Multiselect = false,
                ClientGuid = Guid.Parse("924d6b70-cd7a-48be-8346-d546cc83dfe0"),
                Title = stringLoader.GetString("ExtractDialog.FileDialogTitle"),
            };

            Window window = Window.GetWindow(this);
            bool? dialogResult = browserDialog.ShowDialog(window);
            if (!dialogResult.HasValue || !dialogResult.Value)
            {
                return;
            }

            MsiFile[] filesToExtract = new MsiFile[FileListView.SelectedItems.Count];
            FileListView.SelectedItems.CopyTo(filesToExtract, 0);

            string text;
            if (filesToExtract.Length > 1)
            {
                text = string.Format(stringLoader.GetString("ExtractDialog.Instruction"), filesToExtract.Length);
            }
            else
            {
                text = stringLoader.GetString("ExtractDialog.Instruction.Singular");
            }

            using var progressDialog = new ProgressDialog
            {
                MinimizeBox = false,
                WindowTitle = stringLoader.GetString("ExtractDialog.WindowTitle"),
                UseCompactPathsForDescription = true,
                ShowCancelButton = false,
                Text = text,
            };

            Exception? caughtException = null;

            void DoWork(object? sender, DoWorkEventArgs e)
            {
                if (string.IsNullOrEmpty(model.MsiPath))
                {
                    throw new InvalidOperationException("MsiPath not set, we should not have gotten here");
                }

                Wixtracts.ExtractFiles(new LessIO.Path(model.MsiPath), browserDialog.FolderName, filesToExtract, (arg) =>
                {
                    var progress = (Wixtracts.ExtractionProgress)arg;
                    if (progressDialog.CancellationPending)
                    {
                        throw new OperationCanceledException();
                    }

                    int percentProgress;
                    string message;

                    if (progress.Activity == Wixtracts.ExtractionActivity.Initializing)
                    {
                        message = stringLoader.GetString("ExtractDialog.Message.Preparing");
                        percentProgress = 0;
                    }
                    else if (progress.Activity == Wixtracts.ExtractionActivity.Uncompressing)
                    {
                        message = stringLoader.GetString("ExtractDialog.Message.Decompressing");
                        percentProgress = 0;
                    }
                    else if (progress.Activity == Wixtracts.ExtractionActivity.ExtractingFile)
                    {
                        double fraction = (double)progress.FilesExtractedSoFar / (double)progress.TotalFileCount;
                        percentProgress = (int)Math.Round(fraction * 100);
                        message = progress.CurrentFileName;
                    }
                    else if (progress.Activity == Wixtracts.ExtractionActivity.Complete)
                    {
                        message = stringLoader.GetString("ExtractDialog.Message.Complete");
                        percentProgress = 100;
                    }
                    else
                    {
                        throw new ArgumentException("Invalid ExtractionActivity");
                    }

                    this.Dispatcher.Invoke(() => progressDialog.ReportProgress(percentProgress, null, message));
                });
            }

            void RunWorkerCompleted(object? sender, RunWorkerCompletedEventArgs e)
            {
                this.Dispatcher.Invoke(() =>
                {
                    Exception? ex = e.Error;
                    if (ex != null)
                    {
                        if (ex is FileNotFoundException fnf)
                        {
                            TaskDialogPage page = new TaskDialogPage();
                            page.Title = stringLoader.GetString("ErrorDialog.Title");
                            page.Instruction = stringLoader.GetString("FileNotFoundDialog.Instruction");
                            page.Text = string.Format(stringLoader.GetString("FileNotFoundDialog.Text"), fnf.FileName);
                            page.Icon = TaskDialogIcon.Get(TaskDialogStandardIcon.Error);
                            page.StandardButtons.Add(TaskDialogResult.Close);
                            page.AllowCancel = true;

                            TaskDialog.Show(window, page);
                        }
                        else if (ex is not OperationCanceledException)
                        {
                            TaskDialogPage page = new TaskDialogPage();
                            page.Title = stringLoader.GetString("ErrorDialog.Title");
                            page.Instruction = stringLoader.GetString("ExtractFailureDialog.Instruction");
                            page.Text = string.Format("ExtractFailureDialog.Text", ex.GetType().Name, ex.Message, ex.HResult.ToString("X8"));
                            page.Icon = TaskDialogIcon.Get(TaskDialogStandardIcon.Error);
                            page.StandardButtons.Add(TaskDialogResult.Close);
                            page.AllowCancel = true;

                            TaskDialog.Show(window, page);
                        }
                    }
                    else if (!e.Cancelled)
                    {
                        TaskDialogPage page = new TaskDialogPage();
                        page.Instruction = "Extraction is complete.";
                        page.StandardButtons.Add(TaskDialogResult.OK);
                        page.AllowCancel = true;

                        TaskDialog.Show(window, page);
                    }
                });
            }

            progressDialog.DoWork += DoWork;
            progressDialog.RunWorkerCompleted += RunWorkerCompleted;
            progressDialog.ShowDialog(window);
        }

        private void ExtractCommand_CanExecute(object? sender, CanExecuteRoutedEventArgs e)
        {
            if (DataContext != null)
            {
                var model = (AppModel)DataContext;
                e.CanExecute = !string.IsNullOrEmpty(model.MsiPath) && FileListView.SelectedItems.Count > 0;
            }
            else
            {
                e.CanExecute = false;
            }
        }
    }
}
