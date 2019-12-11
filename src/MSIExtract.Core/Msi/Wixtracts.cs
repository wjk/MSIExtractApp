// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
// Copyright (c) 2004 Scott Willeke (http://scott.willeke.com)
//
// Authors:
//	Scott Willeke (scott@willeke.com)
//
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Threading;
using LessIO;
using MSIExtract.OleStorage;
using WixToolset.Dtf.Compression;
using WixToolset.Dtf.Compression.Cab;
using WixToolset.Dtf.WindowsInstaller;
using Path = LessIO.Path;

namespace MSIExtract.Msi
{
    public class Wixtracts
    {
        #region class ExtractionProgress

        /// <summary>
        /// Provides progress information during an extraction operatation.
        /// </summary>
        public class ExtractionProgress : IAsyncResult
        {
            private string _currentFileName;
            private ExtractionActivity _activity;
            private readonly ManualResetEvent _waitSignal;
            private readonly AsyncCallback _callback;
            private readonly int _totalFileCount;
            private int _filesExtracted;

            public ExtractionProgress(AsyncCallback progressCallback, int totalFileCount)
            {
                _activity = ExtractionActivity.Initializing;
                _currentFileName = "";
                _callback = progressCallback;
                _waitSignal = new ManualResetEvent(false);
                _totalFileCount = totalFileCount;
                _filesExtracted = 0;
            }

            internal void ReportProgress(ExtractionActivity activity, string currentFileName, int filesExtractedSoFar)
            {
                lock (this)
                {
                    _activity = activity;
                    _currentFileName = currentFileName;
                    _filesExtracted = filesExtractedSoFar;

                    if (this.IsCompleted)
                        _waitSignal.Set();

                    if (_callback != null)
                        _callback(this);
                }
            }

            /// <summary>
            /// The total number of files to be extracted for this operation.
            /// </summary>
            public int TotalFileCount
            {
                get
                {
                    lock (this)
                    {
                        return _totalFileCount;
                    }
                }
            }

            /// <summary>
            /// The number of files extracted so far
            /// </summary>
            public int FilesExtractedSoFar
            {
                get
                {
                    lock (this)
                    {
                        return _filesExtracted;
                    }
                }
            }


            /// <summary>
            /// If <see cref="Activity"/> is <see cref="ExtractionActivity.ExtractingFile"/>, specifies the name of the file being extracted.
            /// </summary>
            public string CurrentFileName
            {
                get
                {
                    lock (this)
                    {
                        return _currentFileName;
                    }
                }
            }

            /// <summary>
            /// Specifies the current activity.
            /// </summary>
            public ExtractionActivity Activity
            {
                get
                {
                    lock (this)
                    {
                        return _activity;
                    }
                }
            }

            #region IAsyncResult Members

            object IAsyncResult.AsyncState
            {
                get
                {
                    lock (this)
                    {
                        return this;
                    }
                }
            }

            bool IAsyncResult.CompletedSynchronously
            {
                get
                {
                    lock (this)
                    {
                        return false;
                    }
                }
            }

            public WaitHandle AsyncWaitHandle
            {
                get
                {
                    lock (this)
                    {
                        return _waitSignal;
                    }
                }
            }

            public bool IsCompleted
            {
                get
                {
                    lock (this)
                    {
                        return this.Activity == ExtractionActivity.Complete;
                    }
                }
            }

            #endregion
        }

        #endregion

        #region enum ExtractionActivity

        /// <summary>
        /// Specifies the differernt available activities.
        /// </summary>
        public enum ExtractionActivity
        {
            Initializing,
            Uncompressing,
            ExtractingFile,
            Complete
        }

        #endregion

        public static void ExtractFiles(Path msi, string outputDir)
        {
            ExtractFiles(msi, outputDir, new string[0], null);
        }

        public static void ExtractFiles(Path msi, string outputDir, string[] fileNamesToExtract)
        {
            var msiFiles = GetMsiFileFromFileNames(msi, fileNamesToExtract);
            ExtractFiles(msi, outputDir, msiFiles, null);
        }

        public static void ExtractFiles(Path msi, string outputDir, string[] fileNamesToExtract, AsyncCallback progressCallback)
        {
            var msiFiles = GetMsiFileFromFileNames(msi, fileNamesToExtract);
            ExtractFiles(msi, outputDir, msiFiles, progressCallback);
        }

        private static MsiFile[] GetMsiFileFromFileNames(Path msi, string[] fileNamesToExtract)
        {
            var msiFiles = MsiFile.CreateMsiFilesFromMSI(msi);
            Array.Sort(msiFiles, (f1, f2) => string.Compare(f1.LongFileName, f2.LongFileName, StringComparison.InvariantCulture));

            var fileNamesToExtractAsMsiFiles = new List<MsiFile>();
            foreach (var fileName in fileNamesToExtract)
            {
                var found = Array.BinarySearch(msiFiles, fileName, FileNameComparer.Default);
                if (found >= 0)
                    fileNamesToExtractAsMsiFiles.Add(msiFiles[found]);
                else
                    Console.WriteLine("File {0} was not found in the msi.", fileName);
            }
            return fileNamesToExtractAsMsiFiles.ToArray();
        }

        private sealed class FileNameComparer : IComparer
        {
            static readonly FileNameComparer _default = new FileNameComparer();

            public static FileNameComparer Default
            {
                get { return _default; }
            }

            int IComparer.Compare(object x, object y)
            {
                if (x == null) throw new ArgumentNullException(nameof(x));
                if (y == null) throw new ArgumentNullException(nameof(y));

                // expect two MsiFile or one MsiFile and one string:
                var getName = new Func<object, string>((object fileOrName) => fileOrName is MsiFile ? ((MsiFile)fileOrName).LongFileName : (string)fileOrName);
                var xName = getName(x);
                var yName = getName(y);
                return string.Compare(xName, yName, StringComparison.InvariantCulture);
            }
        }

        private sealed class UnpackContext : IUnpackStreamContext
        {
            public UnpackContext(IReadOnlyList<CabInfo> cabs)
            {
                Cabinets = cabs;
            }

            public Func<string, Stream> CreateFileStream { get; set; }

            public Action<string> FileCompleted { get; set; }

            public IReadOnlyList<CabInfo> Cabinets { get; }

            public Stream OpenArchiveReadStream(int archiveNumber, string archiveName, CompressionEngine compressionEngine)
            {
                return File.Open(Cabinets[archiveNumber].LocalCabFile, FileMode.Open);
            }

            public void CloseArchiveReadStream(int archiveNumber, string archiveName, Stream stream)
            {
                stream.Dispose();
            }

            public Stream OpenFileWriteStream(string path, long fileSize, DateTime lastWriteTime)
            {
                if (CreateFileStream == null) throw new InvalidOperationException("CreateFileStream must be set");
                return CreateFileStream(path);
            }

            public void CloseFileWriteStream(string path, Stream stream, System.IO.FileAttributes attributes, DateTime lastWriteTime)
            {
                stream.Dispose();
                File.SetAttributes(path, attributes);
                File.SetLastWriteTime(path, lastWriteTime);
                FileCompleted?.Invoke(path);
            }
        }

        /// <summary>
        /// Extracts the compressed files from the specified MSI file to the specified output directory.
        /// If specified, the list of <paramref name="filesToExtract"/> objects are the only files extracted.
        /// </summary>
        /// <param name="msi">The MSI file to extract files from.</param>
        /// <param name="outputDir">The directory to extract the files into.</param>
        /// <param name="filesToExtract">The files to extract or null or empty to extract all files.</param>
        /// <param name="progressCallback">Will be called during during the operation with progress information, and upon completion. The argument will be of type <see cref="ExtractionProgress"/>.</param>
        public static void ExtractFiles(Path msi, string outputDir, MsiFile[] filesToExtract, AsyncCallback progressCallback)
        {
            if (msi.IsEmpty)
                throw new ArgumentNullException("msi");
            if (string.IsNullOrEmpty(outputDir))
                throw new ArgumentNullException("outputDir");

            int filesExtractedSoFar = 0;
            //Refrence on Embedding files: https://msdn.microsoft.com/en-us/library/aa369279.aspx
            ExtractionProgress progress = null;
            Database msidb = new Database(msi.PathString, DatabaseOpenMode.ReadOnly);
            try
            {
                if (filesToExtract == null || filesToExtract.Length < 1)
                    filesToExtract = MsiFile.CreateMsiFilesFromMSI(msidb);

                progress = new ExtractionProgress(progressCallback, filesToExtract.Length);

                if (!FileSystem.Exists(msi))
                {
                    Trace.WriteLine("File \'" + msi + "\' not found.");
                    progress.ReportProgress(ExtractionActivity.Complete, string.Empty, filesExtractedSoFar);
                    return;
                }

                progress.ReportProgress(ExtractionActivity.Initializing, string.Empty, filesExtractedSoFar);
                FileSystem.CreateDirectory(new Path(outputDir));

                //map short file names to the msi file entry
                var fileEntryMap = new Dictionary<string, MsiFile>(filesToExtract.Length, StringComparer.InvariantCulture);
                foreach (var fileEntry in filesToExtract)
                {
                    MsiFile existingFile = null;
                    if (fileEntryMap.TryGetValue(fileEntry.File, out existingFile))
                    {
                        //NOTE: This used to be triggered when we ignored case of file, but now we don't ignore case so this is unlikely to occur.
                        // Differing only by case is not compliant with the msi specification but some installers do it (e.g. python, see issue 28).
                        Debug.Print("!!Found duplicate file using key {0}. The existing key was {1}", fileEntry.File, existingFile.File);
                    }
                    else
                    {
                        fileEntryMap.Add(fileEntry.File, fileEntry);
                    }
                }

                Debug.Assert(fileEntryMap.Count == filesToExtract.Length, "Duplicate files must have caused some files to not be in the map.");

                var cabEngine = new CabEngine();
                var cabInfos = CabsFromMsiToDisk(msi, msidb, outputDir);

                var context = new UnpackContext(cabInfos);
                context.CreateFileStream = (nameInCab) =>
                {
                    if (!fileEntryMap.ContainsKey(nameInCab))
                    {
                        // Returning null will instruct Dtf to skip this file.
#pragma warning disable CS8603 // Possible null reference return (false positive)
                        return null;
#pragma warning restore CS8603 // Possible null reference return
                    }

                    progress.ReportProgress(ExtractionActivity.ExtractingFile, nameInCab, filesExtractedSoFar);

                    Path sourcePath = new Path(nameInCab);
                    string targetDirectoryForFile = GetTargetDirectory(outputDir, sourcePath.Parent.PathString);
                    Path destName = Path.Combine(targetDirectoryForFile, sourcePath.Parent.PathString);
                    return File.Open(destName.PathString, FileMode.CreateNew);
                };
                context.FileCompleted = (name) =>
                {
                    filesExtractedSoFar++;
                };

                try
                {
                    cabEngine.Unpack(context, null);
                }
                finally
                {
                    cabEngine.Dispose();
                    foreach (CabInfo cab in cabInfos)
                    {
                        DeleteFileForcefully(new Path(cab.LocalCabFile));
                    }
                }
            }
            finally
            {
                if (msidb != null)
                    msidb.Close();
                if (progress != null)
                    progress.ReportProgress(ExtractionActivity.Complete, "", filesExtractedSoFar);
            }
        }

        /// <summary>
        /// Deletes a file even if it is readonly.
        /// </summary>
        private static void DeleteFileForcefully(Path localFilePath)
        {
            // In github issue #4 found that the cab files in the Win7SDK have the readonly attribute set and File.Delete fails to delete them. Explicitly unsetting that bit before deleting works okay...
            FileSystem.RemoveFile(localFilePath, true);
        }

        private static CabInfo FindCabAndRemoveFromList(IList<CabInfo> cabInfos, string soughtName)
        {
            for (var i = 0; i < cabInfos.Count; i++)
            {
                if (string.Equals(cabInfos[i].CabSourceName, soughtName, StringComparison.InvariantCultureIgnoreCase))
                {
                    var found = cabInfos[i];
                    cabInfos.RemoveAt(i);
                    return found;
                }
            }
            throw new Exception("Specified cab not found!");
        }

        private static string GetTargetDirectory(string rootDirectory, string relativePath)
        {
            LessIO.Path fullPath = LessIO.Path.Combine(rootDirectory, relativePath);
            if (!FileSystem.Exists(fullPath))
            {
                FileSystem.CreateDirectory(fullPath);
            }

            return fullPath.PathString;
        }


        /// <summary>
        /// Extracts cab files from the specified MSIDB and puts them in the specified outputdir.
        /// </summary>
        /// <param name="msi"></param>
        /// <param name="msidb"></param>
        /// <param name="outputDir"></param>
        /// <returns></returns>
        private static List<CabInfo> CabsFromMsiToDisk(Path msi, Database msidb, string outputDir)
        {
            const string query = "SELECT * FROM `Media`";
            var localCabFiles = new List<CabInfo>();
            using (View view = msidb.OpenView(query))
            {
                view.Execute();

                Record record;
                while ((record = view.Fetch()) != null)
                {
                    using (record)
                    {
                        const int MsiInterop_Media_Cabinet = 4;
                        string cabSourceName = (string)record[MsiInterop_Media_Cabinet];
                        if (string.IsNullOrEmpty(cabSourceName))
                        {
                            Debug.Print("Empty Cabinet value in Media table. This happens, but it's rare and it's weird!");
                            //Debug.Fail("Couldn't find media CAB file inside the MSI (bad media table?)."); 
                            continue;
                        }
                        if (!string.IsNullOrEmpty(cabSourceName))
                        {
                            bool extract = false;
                            if (cabSourceName.StartsWith("#"))
                            {
                                extract = true;
                                cabSourceName = cabSourceName.Substring(1);
                            }
                            Path localCabFile = Path.Combine(outputDir, cabSourceName);
                            if (extract)
                            {
                                // extract cabinet, then explode all of the files to a temp directory
                                ExtractCabFromPackage(localCabFile, cabSourceName, msidb, msi);
                            }
                            else
                            {
                                Path originalCabFile = Path.Combine(msi.Parent, cabSourceName);
                                FileSystem.Copy(originalCabFile, localCabFile);
                            }
                            /* http://code.google.com/p/lessmsi/issues/detail?id=1
								* apparently in some cases a file spans multiple CABs (VBRuntime.msi) so due to that we have get all CAB files out of the MSI and then begin extraction. Then after we extract everything out of all CAbs we need to release the CAB extractors and delete temp files.
								* Thanks to Christopher Hamburg for explaining this!
							*/
                            var c = new CabInfo(localCabFile.PathString, cabSourceName);
                            localCabFiles.Add(c);
                        }
                    }
                }
            }
            return localCabFiles;
        }

        class CabInfo
        {
            /// <summary>
            /// Name of the cab in the MSI.
            /// </summary>
            public string CabSourceName { get; set; }
            /// <summary>
            /// Path of the CAB on local disk after we pop it out of the msi.
            /// </summary>
            public string LocalCabFile { get; set; } //TODO: Make LocalCabFile use LessIO.Path

            public CabInfo(string localCabFile, string cabSourceName)
            {
                LocalCabFile = localCabFile;
                CabSourceName = cabSourceName;
            }
        }

        public static void ExtractCabFromPackage(Path destCabPath, string cabName, Database inputDatabase, LessIO.Path msiPath)
        {
            //NOTE: checking inputDatabase.TableExists("_Streams") here is not accurate. It reports that it doesn't exist at times when it is perfectly queryable. So we actually try it and look for a specific exception:
            //NOTE: we do want to tryStreams. It is more reliable when available and AFAICT it always /should/ be there according to the docs but isn't.
            const bool tryStreams = true;
            if (tryStreams)
            {
                try
                {
                    ExtractCabFromPackageTraditionalWay(destCabPath, cabName, inputDatabase);
                    // as long as TraditionalWay didn't throw, we'll leave it at that...
                    return;
                }
                catch (Exception e)
                {
                    Debug.WriteLine("ExtractCabFromPackageTraditionalWay Exception: {0}", e);
                    // According to issue #78 (https://github.com/activescott/lessmsi/issues/78), WIX installers sometimes (always?)
                    // don't have _Streams table yet they still install. Since it appears that msi files generally (BUT NOT ALWAYS - see X86 Debuggers And Tools-x86_en-us.msi) will have only one cab file, we'll try to just find it in the sterams and use it instead:
                    Trace.WriteLine("MSI File has no _Streams table. Attempting alternate cab file extraction process...");
                }
            }

            using (var stg = new OleStorageFile(msiPath))
            {
                // MSIs do exist with >1. If we use the ExtractCabFromPackageTraditionalWay (via _Streams table) then it handles that. If we are using this fallback approach, multiple cabs is a bad sign!
                Debug.Assert(CountCabs(stg) == 1, string.Format("Expected 1 cab, but found {0}.", CountCabs(stg)));
                foreach (var strm in stg.GetStreams())
                {
                    using (var bits = strm.GetStream(FileMode.Open, FileAccess.Read))
                    {
                        if (OleStorageFile.IsCabStream(bits))
                        {
                            Trace.WriteLine(String.Format("Found CAB bits in stream. Assuming it is for cab {0}.", destCabPath));
                            Func<byte[], int> streamReader = destBuffer => bits.Read(destBuffer, 0, destBuffer.Length);
                            CopyStreamToFile(streamReader, destCabPath);
                        }
                    }
                }
            }
        }

        private static int CountCabs(OleStorageFile stg)
        {
            return stg.GetStreams().Count(strm => OleStorageFile.IsCabStream((StreamInfo)strm));
        }

        /// <summary>
        /// Write the Cab to disk.
        /// </summary>
        /// <param name="destCabPath">Specifies the path to the file to contain the stream.</param>
        /// <param name="cabName">Specifies the name of the file in the stream.</param>
        /// <param name="inputDatabase">The MSI database to get cabs from.</param>
        public static void ExtractCabFromPackageTraditionalWay(Path destCabPath, string cabName, Database inputDatabase)
        {
            using (View view = inputDatabase.OpenView("SELECT * FROM `_Streams` WHERE `Name` = '{0}'", cabName))
            {
                view.Execute();

                Record record;
                if ((record = view.Fetch()) != null)
                {
                    using (record)
                    {
                        Func<byte[], int> streamReader = destBuffer =>
                        {
                            const int msiInteropStoragesData = 2; //From wiX:Index to column name Data into Record for row in Msi Table Storages
                            using var stream = record.GetStream(msiInteropStoragesData);
                            var bytesWritten = stream.Read(destBuffer, 0, destBuffer.Length);
                            return bytesWritten;
                        };
                        CopyStreamToFile(streamReader, destCabPath);
                    }
                }
            }
        }

        /// <summary>
        /// Copies the Stream of bytes from the specified streamReader to the specified destination path.
        /// </summary>
        /// <param name="streamReader">
        /// A callback like this:
        /// int StreamReader(byte[] destBuffer)
        /// The function should put bytes into the destBuffer and return the number of bytes written to the buffer.
        /// </param>
        /// <param name="destFile">The file to write the sreamReader's bits to.</param>
        private static void CopyStreamToFile(Func<byte[], int> streamReader, Path destFile)
        {
            using (var writer = new BinaryWriter(FileSystem.CreateFile(destFile)))
            {
                var buf = new byte[1024 * 1024];
                int bytesWritten;
                do
                {
                    bytesWritten = streamReader(buf);
                    if (bytesWritten > 0)
                        writer.Write(buf, 0, bytesWritten);
                } while (bytesWritten > 0);
            }
        }
    }
}
