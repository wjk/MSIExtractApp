// Copyright (c) William Kent. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO.Packaging;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using LessIO;

using BinaryReader = System.IO.BinaryReader;
using File = System.IO.File;
using FileAccess = System.IO.FileAccess;
using FileMode = System.IO.FileMode;
using FileShare = System.IO.FileShare;
using Path = LessIO.Path;
using SeekOrigin = System.IO.SeekOrigin;
using Stream = System.IO.Stream;

namespace MSIExtract.OleStorage
{
    /// <summary>
    /// Represents a Microsoft OLE Structured Storage File (of which MSIs are one of them, but so are things like the old Office binary documents).
    /// </summary>
    public sealed class OleStorageFile : IDisposable
    {
        private readonly Path oleStorageFilePath;
        private readonly StorageInfo storageRoot;
        private bool isDisposed;

        public OleStorageFile(Path oleStorageFilePath)
        {
            int checkResult = NativeMethods.StgIsStorageFile(oleStorageFilePath.FullPathString);
            if (checkResult != 0)
                throw new ArgumentException("The specified file is not an OLE Structured Storage file.");
            this.oleStorageFilePath = oleStorageFilePath;
            this.storageRoot = GetStorageRoot(this.oleStorageFilePath);
            isDisposed = false;
        }

        ~OleStorageFile()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Dispose(bool isDisposing)
        {
            if (isDisposing)
            {
                // don't throw if we're in a finalizer
                ThrowIfDisposed();
            }
            var root = storageRoot;
            if (root == null) return;
            CloseStorageRoot(root);
            isDisposed = true;
        }

        public static void DebugDumpFile(Path fileName)
        {
            using (var storage = new OleStorageFile(fileName))
            {
                storage.Dump();
            }
        }

        private static StorageInfo GetStorageRoot(Path fileName)
        {
            var storageRoot = (StorageInfo)InvokeStorageRootMethod(null,
                "Open", fileName.FullPathString, FileMode.Open, FileAccess.Read, FileShare.Read);
            if (storageRoot == null)
            {
                throw new InvalidOperationException(string.Format("Unable to open \"{0}\" as a structured storage file.", fileName));
            }
            return storageRoot;
        }

        private static void CloseStorageRoot(StorageInfo storageRoot)
        {
            InvokeStorageRootMethod(storageRoot, "Close");
        }

        private static object InvokeStorageRootMethod(StorageInfo storageRoot, string methodName, params object[] methodArgs)
        {
            // from http://www.pinvoke.net/default.aspx/ole32/StgOpenStorageEx.html

            //We need the StorageRoot class to directly open an OSS file.  Unfortunately, it's internal.
            //So we'll have to use Reflection to access it.  This code was inspired by:
            //http://henbo.spaces.live.com/blog/cns!2E073207A544E12!200.entry
            //Note: In early WinFX CTPs the StorageRoot class was public because it was documented
            //here: http://msdn2.microsoft.com/en-us/library/aa480157.aspx

            Type storageRootType = typeof(StorageInfo).Assembly.GetType("System.IO.Packaging.StorageRoot", true, false);
            object result = storageRootType.InvokeMember(methodName,
                BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.InvokeMethod,
                null, storageRoot, methodArgs);
            return result;
        }

        [DebuggerHidden]
        private void ThrowIfDisposed()
        {
            if (isDisposed)
                throw new ObjectDisposedException(GetType().Name);
        }

        public StreamInfo[] GetStreams()
        {
            ThrowIfDisposed();
            return storageRoot.GetStreams();
        }

        public StorageInfo StorageRoot
        {
            get
            {
                ThrowIfDisposed();
                return storageRoot;
            }
        }

        private string BasePath
        {
            get
            {
                return oleStorageFilePath.Parent.FullPathString;
            }
        }

        public void Dump()
        {
            var dumpfileName = Path.Combine(BasePath, Path.GetFileName(oleStorageFilePath.FullPathString) + "_streams.xml").FullPathString;
            using (var writer = XmlWriter.Create(dumpfileName, new XmlWriterSettings { Indent = true }))
            {
                DumpStorage(StorageRoot, "root", writer);
            }
        }

        private void DumpStorage(StorageInfo storageInfo, string storageName, XmlWriter writer)
        {
            writer.WriteStartElement("storage");
            writer.WriteAttributeString("name", storageName);

            StorageInfo[] subStorages = storageInfo.GetSubStorages();
            foreach (StorageInfo subStorage in subStorages)
            {
                DumpStorage(subStorage, subStorage.Name, writer);
            }

            StreamInfo[] streams = storageInfo.GetStreams();
            foreach (StreamInfo stream in streams)
            {
                string hexData = ConvertStreamBytesToHex(stream);
                writer.WriteStartElement("stream");
                using (var rawStream = new BinaryReader(stream.GetStream(FileMode.Open, FileAccess.Read)))
                {
                    var streamsBase = Path.Combine(BasePath, Path.GetFileName(oleStorageFilePath.FullPathString) + "_streams");
                    FileSystem.CreateDirectory(streamsBase);
                    File.WriteAllBytes(Path.Combine(streamsBase, stream.Name).FullPathString, rawStream.ReadBytes((int)rawStream.BaseStream.Length));
                    writer.WriteAttributeString("name", stream.Name);
                    writer.WriteAttributeString("data", hexData);
                    writer.WriteEndElement();
                }
            }

            writer.WriteEndElement();
        }

        private static string ConvertStreamBytesToHex(StreamInfo streamInfo)
        {
            using (var bits = streamInfo.GetStream(FileMode.Open, FileAccess.Read))
            {
                var sb = new StringBuilder();
                int currentRead;
                while ((currentRead = bits.ReadByte()) >= 0)
                {
                    var currentByte = (byte)currentRead;
                    sb.AppendFormat("{0:X2}", currentByte);
                }
                return sb.ToString();
            }
        }

        public static bool IsCabStream(StreamInfo si)
        {
            if (si == null)
                throw new ArgumentNullException();
            using (var bits = si.GetStream())
            {
                return IsCabStream(bits);
            }
        }

        internal static bool IsCabStream(Stream bits)
        {
            byte[] cabHeaderBits = "MSCF".ToCharArray().Select(c => (byte)c).ToArray();
            var buffer = new byte[cabHeaderBits.Length];
            bits.Read(buffer, 0, buffer.Length);
            bits.Seek(0, SeekOrigin.Begin);
            return cabHeaderBits.SequenceEqual(buffer);
        }
    }
}
