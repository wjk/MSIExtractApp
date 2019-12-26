// Copyright (c) William Kent. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

#pragma warning disable SA1600 // Elements should be documented (not worth it, not public API)

namespace MSIExtract.ShellExtension.Interop
{
    [ComImport]
    [Guid("a08ce4d0-fa25-44ab-b57c-c7b1c323e0b9")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IExplorerCommand
    {
        void GetTitle(IShellItemArray itemArray, [MarshalAs(UnmanagedType.LPWStr)] out string? title);

        void GetIcon(IShellItemArray itemArray, [MarshalAs(UnmanagedType.LPWStr)] out string? resourceString);

        void GetToolTip(IShellItemArray itemArray, [MarshalAs(UnmanagedType.LPWStr)] out string? tooltip);

        void GetCanonicalName(out Guid guid);

        void GetState(IShellItemArray itemArray, [MarshalAs(UnmanagedType.Bool)] bool okToBeShow, out ExplorerCommandState commandState);

        void Invoke(IShellItemArray itemArray, [MarshalAs(UnmanagedType.Interface)] object bindCtx);

        void GetFlags(out int flags);

        void EnumSubCommands(out IEnumExplorerCommand commandEnum);
    }
}
