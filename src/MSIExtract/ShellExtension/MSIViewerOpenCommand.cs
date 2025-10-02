﻿// Copyright (c) William Kent. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using ShellCommandLib;
using Windows.ApplicationModel;

namespace MSIExtract.ShellExtension
{
    /// <summary>
    /// Implements the "Open in MSI Viewer" Windows Shell command.
    /// </summary>
    [GeneratedComClass]
    [Guid("754de965-4071-4a89-9620-cf6fe858dca2")]
    public sealed partial class MSIViewerOpenCommand : ExplorerCommandBase
    {
        /// <inheritdoc/>
        public override ExplorerCommandFlag Flags => ExplorerCommandFlag.Default;

        /// <inheritdoc/>
        public override ExplorerCommandState GetState(IEnumerable<string> selectedFiles)
        {
            ArgumentNullException.ThrowIfNull(selectedFiles);
            return selectedFiles.Any(IsMSIFile) ? ExplorerCommandState.Enabled : ExplorerCommandState.Hidden;
        }

        /// <inheritdoc/>
        public override string? GetTitle(IEnumerable<string> selectedFiles) => "Open in MSI Viewer";

        /// <inheritdoc/>
        public override string? GetToolTip(IEnumerable<string> selectedFiles) => null;

        /// <inheritdoc/>
        public override void Invoke(IEnumerable<string> selectedFiles)
        {
            ArgumentNullException.ThrowIfNull(selectedFiles);

            foreach (string msiPath in selectedFiles.Where(IsMSIFile))
            {
                string exePath = Path.Combine(Package.Current.InstalledLocation.Path, "MSIExtract", "MSIExtract.exe");

                var processInfo = new ProcessStartInfo();
                processInfo.FileName = exePath;
                processInfo.Arguments = $"\"{msiPath}\"";
                processInfo.WindowStyle = ProcessWindowStyle.Normal;

                var process = Process.Start(processInfo);
                process?.Dispose();
            }
        }

        private static bool IsMSIFile(string path) => Path.GetExtension(path) == ".msi" || Path.GetExtension(path) == ".msm";
    }
}
