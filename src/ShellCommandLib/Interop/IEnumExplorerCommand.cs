// Copyright (c) William Kent. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

#pragma warning disable SA1600 // Elements should be documented (not worth it, interop)

namespace ShellCommandLib.Interop
{
    [GeneratedComInterface]
    [Guid("a88826f8-186f-4987-aade-ea0cef8fbfe8")]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1601:Partial elements should be documented", Justification = "Interop")]
    public partial interface IEnumExplorerCommand
    {
        [PreserveSig]
        int Next(uint elementCount, [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.Interface, SizeParamIndex = 0)] out IExplorerCommand[] commands, out uint fetched);

        void Skip(uint count);

        void Reset();

        void Clone(out IEnumExplorerCommand copy);
    }
}
