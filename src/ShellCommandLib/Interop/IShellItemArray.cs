// Copyright (c) William Kent. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

#pragma warning disable SA1600 // Elements should be documented (not worth it, interop)

namespace ShellCommandLib.Interop
{
    [GeneratedComInterface]
    [Guid("b63ea76d-1f85-456f-a19c-48159efa858b")]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1601:Partial elements should be documented", Justification = "Interop")]
    public partial interface IShellItemArray
    {
        [Obsolete("Not implemented.")]
        void BindToHandler();

        [Obsolete("Not implemented.")]
        void GetPropertyStore();

        [Obsolete("Not implementeduse.")]
        void GetPropertyDescriptionList();

        [Obsolete("Not implemented.")]
        void GetAttributes();

        void GetCount(out int count);

        void GetItemAt(int index, out IShellItem item);

        [Obsolete("Not implemented.")]
        void EnumItems();
    }
}
