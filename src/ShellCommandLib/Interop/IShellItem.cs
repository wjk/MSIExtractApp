// Copyright (c) William Kent. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

#pragma warning disable SA1600 // Elements should be documented (not worth it, interop)
#pragma warning disable SA1602 // Enumeration items should be documented (ditto)

namespace ShellCommandLib.Interop
{
    [GeneratedComInterface]
    [Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE")]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1601:Partial elements should be documented", Justification = "Interop")]
    public partial interface IShellItem
    {
        [return: MarshalAs(UnmanagedType.Interface)]
        object BindToHandler([MarshalAs(UnmanagedType.Interface)] object pbc, ref Guid bhid, in Guid riid);

        void GetParent([MarshalAs(UnmanagedType.Interface)] out IShellItem ppsi);

        void GetDisplayName(SIGDN sigdnName, [MarshalAs(UnmanagedType.LPWStr)] out string ppszName);

        void GetAttributes(uint sfgaoMask, out uint psfgaoAttribs);

        void Compare([MarshalAs(UnmanagedType.Interface)] IShellItem psi, uint hint, out int piOrder);
    }
}
