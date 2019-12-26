// Copyright (c) William Kent. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

#pragma warning disable SA1600 // Elements should be documented (not worth it, not public API)

namespace MSIExtract.ShellExtension.Interop
{
    [ComImport]
    [Guid("a88826f8-186f-4987-aade-ea0cef8fbfe8")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IEnumExplorerCommand
    {
        [PreserveSig]
        int Next(uint elementCount, [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.Interface, SizeParamIndex = 0)] out IExplorerCommand[] commands, out uint fetched);

        void Skip(uint count);

        void Reset();

        void Clone(out IEnumExplorerCommand copy);
    }
}
