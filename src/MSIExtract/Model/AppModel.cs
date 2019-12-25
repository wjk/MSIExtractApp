// Copyright (c) William Kent. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text;
using MRULib;
using MRULib.MRU.Interfaces;
using MRULib.MRU.Models.Persist;
using MSIExtract.Msi;

namespace MSIExtract
{
    /// <summary>
    /// Provides the model backing the <see cref="MainWindow"/>.
    /// </summary>
    public sealed class AppModel : INotifyPropertyChanged
    {
        private string? msiPath;

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
        }

        /// <summary>
        /// Raised when a property on this class is changed.
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Gets or sets the absolute path to the MSI file being read.
        /// </summary>
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
                    MsiFile[] msiFiles = MsiFile.CreateMsiFilesFromMSI(new LessIO.Path(msiPath));
                    Files = new ObservableCollection<MsiFile>(msiFiles);

                    MRUModel.UpdateEntry(msiPath);
                    SaveMRU();
                }
                else
                {
                    Files = new ObservableCollection<MsiFile>();
                }

                OnPropertyChanged(nameof(MsiPath));
                OnPropertyChanged(nameof(Files));
                OnPropertyChanged(nameof(MRUModel));
            }
        }

        /// <summary>
        /// Gets a collection of the files installed by the MSI.
        /// </summary>
        public ObservableCollection<MsiFile> Files { get; private set; } = new ObservableCollection<MsiFile>();

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
    }
}
