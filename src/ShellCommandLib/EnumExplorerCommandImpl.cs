// Copyright (c) William Kent. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

using ShellCommandLib.Interop;

namespace ShellCommandLib;

[GeneratedComClass]
[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:Elements should be documented", Justification = "Not public API")]
[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1601:Partial elements should be documented", Justification = "Not public API")]
internal partial class EnumExplorerCommandImpl : IEnumExplorerCommand
{
    private readonly ExplorerCommandBase[] commands;
    private uint index;

    public EnumExplorerCommandImpl(IEnumerable<ExplorerCommandBase> commands)
    {
        this.commands = commands.ToArray();
    }

    public int Next(uint elementCount, [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.Interface, SizeParamIndex = 0)] out IExplorerCommand[] commands, out uint fetched)
    {
        const int S_OK = 0, S_FALSE = 1;

        fetched = Math.Min(elementCount, (uint)this.commands.Length - this.index);
        if (fetched == 0)
        {
            commands = Array.Empty<IExplorerCommand>();
            return S_FALSE;
        }

        commands = new IExplorerCommand[fetched];

        for (uint i = 0; i < fetched; i++)
        {
            commands[i] = this.commands[this.index + i];
        }

        this.index += fetched;
        return this.index == this.commands.Length ? S_FALSE : S_OK;
    }

    public void Skip(uint count)
    {
        this.index = Math.Min(this.index + count, (uint)this.commands.Length);
    }

    public void Reset()
    {
        this.index = 0;
    }

    public void Clone(out IEnumExplorerCommand copy)
    {
        EnumExplorerCommandImpl copyImpl = new EnumExplorerCommandImpl(this.commands);
        copyImpl.index = this.index;
        copy = copyImpl;
    }
}
