// Copyright (c) William Kent. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MSIExtract.Controls
{
    /// <summary>
    /// Specifies what kind of string <see cref="FileNameConverter"/> returns.
    /// </summary>
    public enum FileNameDisplayMode
    {
        /// <summary>
        /// The Windows shell default.
        /// </summary>
        Default,

        /// <summary>
        /// An absolute file system path.
        /// </summary>
        FullPath,

        /// <summary>
        /// The name of the file only.
        /// </summary>
        NameOnly,
    }
}
