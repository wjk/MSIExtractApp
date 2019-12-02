// Copyright (c) William Kent. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace LessMsi.OleStorage
{
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:Elements should be documented", Justification = "Not a public API")]
    internal static class NativeMethods
    {
        [DllImport("ole32.dll")]
        internal static extern int StgIsStorageFile([MarshalAs(UnmanagedType.LPWStr)] string pwcsName);
    }
}
