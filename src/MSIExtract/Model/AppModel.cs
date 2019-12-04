﻿// Copyright (c) William Kent. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MSIExtract
{
    /// <summary>
    /// Provides the model backing the <see cref="MainWindow"/>.
    /// </summary>
    public sealed class AppModel
    {
        /// <summary>
        /// Gets or sets a <see cref="FileInfo"/> instance corresponding to the MSI file being read.
        /// </summary>
        public FileInfo? MsiFile { get; set; } = null;
    }
}