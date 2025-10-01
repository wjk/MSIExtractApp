﻿using System;
using System.Runtime.Versioning;

namespace Shmuelie.WinRTServer;

/// <summary>
/// Base for a COM class factory for a .NET type.
/// </summary>
/// <remarks>Does not support aggregation. Will always return <c>CLASS_E_NOAGGREGATION</c> if requested.</remarks>
[SupportedOSPlatform("windows6.0.6000")]
public abstract class BaseClassFactory
{
    /// <summary>
    /// Creates an instance of the object.
    /// </summary>
    /// <returns>An instance of the object.</returns>
    protected internal abstract object CreateInstance();

    /// <summary>
    /// Occurs when a new instance is created.
    /// </summary>
    public event EventHandler<InstanceCreatedEventArgs>? InstanceCreated;

    /// <summary>
    /// Gets the <c>CLSID</c>.
    /// </summary>
    protected internal abstract Guid Clsid
    {
        get;
    }

    /// <summary>
    /// Gets the <c>IID</c>.
    /// </summary>
    protected internal abstract Guid Iid
    {
        get;
    }

    /// <summary>
    /// Raises the <see cref="InstanceCreated"/> event.
    /// </summary>
    /// <param name="instance">The created instance.</param>
    /// <event cref="InstanceCreated"/>
    internal void OnInstanceCreated(object instance)
    {
        InstanceCreated?.Invoke(this, new InstanceCreatedEventArgs(instance));
    }
}
