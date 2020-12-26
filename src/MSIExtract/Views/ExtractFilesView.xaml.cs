// Copyright (c) William Kent. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
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
using MSIExtract.Controls;
using MSIExtract.Msi;

using FolderBrowserDialog = System.Windows.Forms.FolderBrowserDialog;
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

        private void ExtractCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (FileListView.SelectedItems.Count == 0)
            {
                // Shouldn't happen (due to ExtractCommand_CanExecute), but check anyway.
                return;
            }

            AppModel model = (AppModel)DataContext;
            using var browserDialog = new FolderBrowserDialog
            {
                ShowNewFolderButton = true,
                UseDescriptionForTitle = true,
                Description = "Select folder to extract to",
            };

            Window window = Window.GetWindow(this);
            if (!browserDialog.ShowDialog(window))
            {
                return;
            }

            MsiFile[] filesToExtract = new MsiFile[FileListView.SelectedItems.Count];
            FileListView.SelectedItems.CopyTo(filesToExtract, 0);

            string text = $"Extracting {filesToExtract.Length} files...";
            if (filesToExtract.Length == 1)
            {
                text = "Extracting one file...";
            }

            using var progressDialog = new ProgressDialog
            {
                MinimizeBox = false,
                WindowTitle = "Extracting Files...",
                CancellationText = "Canceling...",
                UseCompactPathsForDescription = true,
                Text = text,
            };

            void DoWork(object sender, DoWorkEventArgs e)
            {
                if (string.IsNullOrEmpty(model.MsiPath))
                {
                    throw new InvalidOperationException("MsiPath not set, we should not have gotten here");
                }

                try
                {
                    Wixtracts.ExtractFiles(new LessIO.Path(model.MsiPath), browserDialog.SelectedPath, filesToExtract, (arg) =>
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
                            message = "Initializing extraction";
                            percentProgress = 0;
                        }
                        else if (progress.Activity == Wixtracts.ExtractionActivity.Uncompressing)
                        {
                            message = "Decompressing CAB file";
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
                            message = "Finishing up";
                            percentProgress = 100;
                        }
                        else
                        {
                            throw new ArgumentException("Invalid ExtractionActivity");
                        }

                        this.Dispatcher.Invoke(() => progressDialog.ReportProgress(percentProgress, null, message));
                    });
                }
                catch (System.IO.FileNotFoundException ex)
                {
#pragma warning disable SA1130 // Use lambda syntax (not valid syntax here for some reason)
                    Dispatcher.BeginInvoke((Action)delegate
#pragma warning restore SA1130 // Use lambda syntax
                    {
                        TaskDialogPage page = new TaskDialogPage();
                        page.Instruction = "Cannot extract the specified files.";
                        page.Text = $"The file \"{ex.FileName}\" was not found.";
                        page.Icon = TaskDialogIcon.Get(TaskDialogStandardIcon.Error);
                        page.StandardButtons.Add(TaskDialogResult.Close);
                        page.AllowCancel = true;

                        TaskDialog dialog = new TaskDialog();
                        dialog.Page = page;
                        dialog.Show(window);
                    });

                    return;
                }
                catch (Exception ex)
                {
                    // Rethrow the exception on the main thread.
                    Dispatcher.Invoke(() => throw ex);
                }
            }

            progressDialog.DoWork += DoWork;
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
