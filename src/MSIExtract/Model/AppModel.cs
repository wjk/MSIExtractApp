﻿// Copyright (c) William Kent. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows.Data;
using MRULib;
using MRULib.MRU.Interfaces;
using MRULib.MRU.Models.Persist;
using MSIExtract.Msi;
using MSIExtract.Views;

namespace MSIExtract
{
    /// <summary>
    /// Provides the model backing the <see cref="Views.MainWindow"/>.
    /// </summary>
    public sealed class AppModel : INotifyPropertyChanged
    {
        private readonly CollectionViewSource fileList;
        private string? msiPath;
        private string? filterText;

        /// <summary>
        /// Initializes a new instance of the <see cref="AppModel"/> class.
        /// </summary>
        public AppModel()
        {
            string dirPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MSI Viewer");
            if (!Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
            }

            string persistPath = Path.Combine(dirPath, "mru.dat");
            try
            {
                MRUModel = MRUEntrySerializer.Load(persistPath);
            }
            catch (FileNotFoundException)
            {
                MRUModel = MRU_Service.Create_List();
            }

            fileList = new CollectionViewSource();
            fileList.Filter += FileList_Filter;
        }

        /// <summary>
        /// Raised when a property on this class is changed.
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Gets or sets the absolute path to the MSI file being read.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Catch-all to avoid crashing program")]
        public string? MsiPath
        {
            get
            {
                return msiPath;
            }

            set
            {
                msiPath = value;

                if (msiPath != null)
                {
                    MsiDataContainer container;

                    try
                    {
                        container = MsiDataContainer.CreateFromPath(new LessIO.Path(msiPath));
                    }
                    catch
                    {
                        MainWindow.ShowInvalidFileErrorCommand.Execute(value, null);
                        return;
                    }

                    fileList.Source = container.Files;
                    Tables = new ObservableCollection<TableWithData>(container.Tables);

                    MRUModel.UpdateEntry(msiPath);
                    SaveMRU();
                }
                else
                {
                    fileList.Source = Array.Empty<MsiFile>();
                    Tables = new ObservableCollection<TableWithData>();
                }

                OnPropertyChanged(nameof(MsiPath));
                OnPropertyChanged(nameof(IsMsiLoaded));
                OnPropertyChanged(nameof(Files));
                OnPropertyChanged(nameof(Tables));
                OnPropertyChanged(nameof(MRUModel));
            }
        }

        /// <summary>
        /// Gets a value indicating whether there is an MSI file loaded.
        /// </summary>
        public bool IsMsiLoaded { get => !string.IsNullOrEmpty(this.msiPath); }

        /// <summary>
        /// Gets a collection of the files installed by the MSI.
        /// </summary>
        public ICollectionView Files { get => fileList.View; }

        /// <summary>
        /// Gets or Sets the text that will be used to filter the filelist.
        /// </summary>
        public string FilterText
        {
            get => filterText ?? string.Empty;

            set
            {
                this.filterText = value;
                this.fileList.View.Refresh();
                OnPropertyChanged(nameof(FilterText));
            }
        }

        /// <summary>
        /// Gets a collection of the table names from the MSI.
        /// </summary>
        public ObservableCollection<TableWithData> Tables { get; private set; } = new ObservableCollection<TableWithData>();

        /// <summary>
        /// Gets an <see cref="IMRUListViewModel"/> object that contains the list of recently opened MSI files.
        /// </summary>
        public IMRUListViewModel MRUModel { get; }

        /// <summary>
        /// Removes all entries from the <see cref="MRUModel"/>.
        /// </summary>
        public void ClearMRU()
        {
            MRUModel.Clear();
            SaveMRU();

            OnPropertyChanged(nameof(MRUModel));
        }

        /// <summary>
        /// Removes an <see cref="IMRUEntryViewModel"/> from the <see cref="MRUModel"/>.
        /// </summary>
        /// <param name="entry">
        /// The <see cref="IMRUEntryViewModel"/> to remove.
        /// </param>
        public void RemoveMRUItem(IMRUEntryViewModel entry)
        {
            MRUModel.RemoveEntry(entry);
            SaveMRU();

            OnPropertyChanged(nameof(MRUModel));
        }

        private void SaveMRU()
        {
            string dirPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MSI Viewer");
            if (!Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
            }

            string persistPath = Path.Combine(dirPath, "mru.dat");
            MRUEntrySerializer.Save(persistPath, MRUModel);
        }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void FileList_Filter(object sender, FilterEventArgs e)
        {
            if (string.IsNullOrEmpty(FilterText))
            {
                e.Accepted = true;
                return;
            }

            if (e.Item is MsiFile file)
            {
                var filenameMatch = file.LongFileName.Contains(FilterText, StringComparison.OrdinalIgnoreCase);
                e.Accepted = filenameMatch || file.Directory.FullPath.Contains(FilterText, StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                e.Accepted = false;
            }
        }
    }
}
