﻿using System;

namespace Shmuelie.WinRTServer;

/// <summary>
/// The event data for <see cref="ComServer.InstanceCreated"/>.
/// </summary>
/// <param name="instance">The created instance.</param>
/// <seealso cref="EventArgs"/>
public sealed class InstanceCreatedEventArgs(object instance) : EventArgs
{
    /// <summary>
    /// Gets the created instance.
    /// </summary>
    public object Instance
    {
        get;
    } = instance;
}
