﻿using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using System.Timers;
using Shmuelie.WinRTServer.Internal;
using Shmuelie.WinRTServer.Internal.Windows;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;
using Windows.Win32.System.WinRT;
using static Windows.Win32.PInvoke;
using unsafe DllActivationCallback = delegate* unmanaged[Stdcall]<Windows.Win32.System.WinRT.HSTRING, Windows.Win32.System.WinRT.IActivationFactory**, Windows.Win32.Foundation.HRESULT>;

namespace Shmuelie.WinRTServer;

/// <summary>
/// An Out of Process Windows Runtime Server.
/// </summary>
/// <remarks>
/// <para>Allows for types to be created using WinRT activation instead of COM activation like <see cref="ComServer"/>.</para>
/// <para>Typical usage is to call from an <see langword="await"/> <see langword="using"/> block, using <see cref="WaitForFirstObjectAsync"/> to not close until it is safe to do so.</para>
/// <code language="cs">
/// <![CDATA[
/// await using (WinRtServer server = new WinRtServer())
/// {
///     server.RegisterClass<RemoteThing>();
///     server.Start();
///     await server.WaitForFirstObjectAsync();
/// }
/// ]]>
/// </code>
/// </remarks>
/// <see cref="IAsyncDisposable"/>
/// <threadsafety static="true" instance="false"/>
[SupportedOSPlatform("windows8.0")]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1724", Justification = "No better idea")]
public sealed class WinRtServer : IAsyncDisposable
{
    /// <summary>
    /// Mapping of Activatable Class IDs to activation factories and their <see cref="ComWrappers"/> implementation.
    /// </summary>
    private readonly Dictionary<string, (BaseActivationFactory Factory, ComWrappers Wrapper)> factories = [];

    private readonly unsafe DllGetActivationFactory activationFactoryCallbackWrapper;

    private unsafe readonly DllActivationCallback activationFactoryCallbackPointer;
    private readonly StrategyBasedComWrappers comWrappers = new();

    /// <summary>
    /// Collection of created instances.
    /// </summary>
    private readonly LinkedList<WeakReference> liveServers = new();

    /// <summary>
    /// Timer that checks if all created instances have been collected.
    /// </summary>
    private readonly Timer lifetimeCheckTimer;

    /// <summary>
    /// Tracks the creation of the first instance after server is started.
    /// </summary>
    private TaskCompletionSource<object>? firstInstanceCreated;

    private RO_REGISTRATION_COOKIE registrationCookie = (RO_REGISTRATION_COOKIE)0;

    /// <summary>
    /// Initializes a new instance of the <see cref="WinRtServer"/> class.
    /// </summary>
    public unsafe WinRtServer()
    {
        activationFactoryCallbackWrapper = ActivationFactoryCallback;
        activationFactoryCallbackPointer = (DllActivationCallback)Marshal.GetFunctionPointerForDelegate(activationFactoryCallbackWrapper);

        HRESULT result = RoInitialize(RO_INIT_TYPE.RO_INIT_MULTITHREADED);
        if (result != HRESULT.S_OK && result != HRESULT.S_FALSE)
        {
            result.ThrowOnFailure();
        }

        using ComPtr<IGlobalOptions> options = default;
        Guid clsid = CLSID_GlobalOptions;
        Guid iid = IGlobalOptions.IID_Guid;
        if (CoCreateInstance(&clsid, null, CLSCTX.CLSCTX_INPROC_SERVER, &iid, (void**)options.GetAddressOf()) == HRESULT.S_OK)
        {
            options.Get()->Set(GLOBALOPT_PROPERTIES.COMGLB_RO_SETTINGS, (nuint)GLOBALOPT_RO_FLAGS.COMGLB_FAST_RUNDOWN);
        }

        lifetimeCheckTimer = new()
        {
            Interval = 60000,
        };
        lifetimeCheckTimer.Elapsed += LifetimeCheckTimer_Elapsed;
    }

    /// <summary>
    /// Handles <see cref="Timer.Elapsed"/> event from <see cref="lifetimeCheckTimer"/>.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">An <see cref="ElapsedEventArgs"/> object that contains the event data.</param>
    private void LifetimeCheckTimer_Elapsed(object? sender, ElapsedEventArgs e)
    {
        if (IsDisposed)
        {
            return;
        }

        GC.Collect();
        for (LinkedListNode<WeakReference>? node = liveServers.First; node != null; node = node.Next)
        {
            if (!node.Value.IsAlive)
            {
                var previous = node.Previous;
                liveServers.Remove(node);
                if (previous is null)
                {
                    break;
                }
                node = previous;
            }
        }

        if (liveServers.Count == 0)
        {
            Empty?.Invoke(this, EventArgs.Empty);
        }
    }

    private void Factory_InstanceCreated(object? sender, InstanceCreatedEventArgs e)
    {
        if (IsDisposed)
        {
            return;
        }

        liveServers.AddLast(new WeakReference(e.Instance));
        InstanceCreated?.Invoke(this, e);
        firstInstanceCreated?.TrySetResult(e.Instance);
    }

    /// <summary>
    /// Register an activation factory with the server.
    /// </summary>
    /// <param name="factory">The activation factory to register.</param>
    /// <param name="comWrappers">The implementation of <see cref="ComWrappers"/> to use for wrapping.</param>
    /// <returns><see langword="true"/> if <paramref name="factory"/> was registered; otherwise, <see langword="false"/>.</returns>
    /// <remarks>Only one factory can be registered for a Activatable Class ID.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="factory"/> or <paramref name="comWrappers"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">The server is running.</exception>
    public bool RegisterActivationFactory(BaseActivationFactory factory, ComWrappers comWrappers)
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        if (IsRunning)
        {
            throw new InvalidOperationException("Can only add activation factories when server is not running");
        }
        ArgumentNullException.ThrowIfNull(factory);
        ArgumentNullException.ThrowIfNull(comWrappers);

        if (factories.ContainsKey(factory.ActivatableClassId))
        {
            return false;
        }

        factories.Add(factory.ActivatableClassId, (factory, comWrappers));
        return true;
    }

    /// <summary>
    /// Unregister an activation factory with the server.
    /// </summary>
    /// <param name="factory">The activation factory to unregister.</param>
    /// <returns><see langword="true"/> if <paramref name="factory"/> was unregistered; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="factory"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">The server is running.</exception>
    public bool UnregisterActivationFactory(BaseActivationFactory factory)
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        if (IsRunning)
        {
            throw new InvalidOperationException("Can only remove activation factories when server is not running");
        }
        ArgumentNullException.ThrowIfNull(factory);

        return factories.Remove(factory.ActivatableClassId);
    }

    private unsafe HRESULT ActivationFactoryCallback(HSTRING activatableClassId, IActivationFactory** factory)
    {
        if (activatableClassId == HSTRING.Null || factory is null)
        {
            return HRESULT.E_INVALIDARG;
        }

        if (!factories.TryGetValue(activatableClassId.AsString(), out var managedFactory))
        {
            factory = null;
            return HRESULT.E_NOINTERFACE;
        }

        var unknown = comWrappers.GetOrCreateComInterfaceForObject(new BaseActivationFactoryWrapper(managedFactory.Factory, managedFactory.Wrapper), CreateComInterfaceFlags.None);
        var hr = (HRESULT)Marshal.QueryInterface(unknown, in global::Windows.Win32.System.WinRT.IActivationFactory.IID_Guid, out nint ppv);
        *factory = (IActivationFactory*)ppv;
        if (unknown != 0)
        {
            Marshal.Release(unknown);
        }

        return hr;
    }

    /// <summary>
    /// Gets a value indicating whether the instance is disposed.
    /// </summary>
    public bool IsDisposed
    {
        get;
        private set;
    }

    /// <summary>
    /// Gets a value indicating whether the server is running.
    /// </summary>
    public bool IsRunning => registrationCookie != 0;

    /// <summary>
    /// Starts the server.
    /// </summary>
    /// <remarks>Calling <see cref="Start"/> is non-blocking.</remarks>
    public unsafe void Start()
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        if (IsRunning)
        {
            return;
        }

        string[] managedActivatableClassIds = [.. factories.Keys];
        HSTRING* activatableClassIds = null;
        DllActivationCallback* activationFactoryCallbacks = null;
        try
        {
            activatableClassIds = (HSTRING*)Marshal.AllocHGlobal(sizeof(HSTRING) * managedActivatableClassIds.Length);
            for (int activatableClassIdIndex = 0; activatableClassIdIndex < managedActivatableClassIds.Length; activatableClassIdIndex++)
            {
                string managedActivatableClassId = managedActivatableClassIds[activatableClassIdIndex];
                fixed (char* managedActivatableClassIdPtr = managedActivatableClassId)
                {
                    WindowsCreateString((PCWSTR)managedActivatableClassIdPtr, (uint)managedActivatableClassId.Length, &activatableClassIds[activatableClassIdIndex]).ThrowOnFailure();
                }
            }

            activationFactoryCallbacks = (DllActivationCallback*)Marshal.AllocHGlobal(sizeof(DllActivationCallback*) * managedActivatableClassIds.Length);
            for (int activationFactoryCallbackIndex = 0; activationFactoryCallbackIndex < managedActivatableClassIds.Length; activationFactoryCallbackIndex++)
            {
                activationFactoryCallbacks[activationFactoryCallbackIndex] = activationFactoryCallbackPointer;
            }

            fixed (RO_REGISTRATION_COOKIE* cookie = &registrationCookie)
            {
                RoRegisterActivationFactories(activatableClassIds, activationFactoryCallbacks, (uint)managedActivatableClassIds.Length, cookie).ThrowOnFailure();
            }
        }
        finally
        {
            if (activationFactoryCallbacks is not null)
            {
                Marshal.FreeHGlobal((IntPtr)activationFactoryCallbacks);
            }
            if (activatableClassIds is not null)
            {
                for (int activatableClassIdIndex = 0; activatableClassIdIndex < managedActivatableClassIds.Length; activatableClassIdIndex++)
                {
                    _ = WindowsDeleteString(activatableClassIds[activatableClassIdIndex]);
                }
                Marshal.FreeHGlobal((IntPtr)activatableClassIds);
            }
        }

        firstInstanceCreated = new();
        lifetimeCheckTimer.Start();
    }

    /// <summary>
    /// Stops the server.
    /// </summary>
    public void Stop()
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        if (!IsRunning)
        {
            return;
        }

        RoRevokeActivationFactories(registrationCookie);
        registrationCookie = (RO_REGISTRATION_COOKIE)0;

        firstInstanceCreated = null;
        lifetimeCheckTimer.Stop();
    }

    /// <summary>
    /// Wait for all objects created by the server to be deallocated.
    /// </summary>
    /// <returns>
    /// A <see cref="Task"/> to await.
    /// </returns>
    /// <exception cref="ObjectDisposedException">The instance is disposed.</exception>
    /// <exception cref="InvalidOperationException">Thrown if <see cref="Start"/> has not yet been called.</exception>
    public async Task WaitForRunDown()
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        if (!IsRunning)
        {
            throw new InvalidOperationException("WaitForRunDown() cannot be called if the WinRtServer is stopped");
        }

        TaskCompletionSource taskCompletionSource = new();
        void OnEmpty(object? sender, EventArgs e)
        {
            Empty -= OnEmpty;
            taskCompletionSource.SetResult();
        }

        Empty += OnEmpty;
        await taskCompletionSource.Task.ConfigureAwait(false);
    }

    /// <summary>
    /// Wait for the server to have created an object since it was started.
    /// </summary>
    /// <returns>The first object created if the server is running; otherwise <see langword="null"/>.</returns>
    /// <exception cref="ObjectDisposedException">The instance is disposed.</exception>
    public async Task<object?> WaitForFirstObjectAsync()
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);

        TaskCompletionSource<object>? local = firstInstanceCreated;
        if (local is null)
        {
            return null;
        }
        return await local.Task.ConfigureAwait(false);
    }

    /// <summary>
    /// Force the server to stop and release all resources.
    /// </summary>
    /// <remarks>Unlike <see cref="DisposeAsync"/>, <see cref="UnsafeDispose"/> will ignore if any objects are still alive before unregistering activation factories.</remarks>
    /// <seealso cref="DisposeAsync"/>
    public void UnsafeDispose()
    {
        if (!IsDisposed)
        {
            try
            {
                liveServers.Clear();
                lifetimeCheckTimer.Stop();
                lifetimeCheckTimer.Dispose();

                RoRevokeActivationFactories(registrationCookie);
                registrationCookie = (RO_REGISTRATION_COOKIE)0;
            }
            finally
            {
                IsDisposed = true;
            }
        }
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (!IsDisposed)
        {
            try
            {
                if (liveServers.Count != 0)
                {
                    TaskCompletionSource<bool> tcs = new();
                    void Ended(object? sender, EventArgs e)
                    {
                        tcs.SetResult(true);
                    }

                    Empty += Ended;
                    await tcs.Task.ConfigureAwait(false);
                    Empty -= Ended;
                }

                lifetimeCheckTimer.Stop();
                lifetimeCheckTimer.Dispose();

                RoRevokeActivationFactories(registrationCookie);
                registrationCookie = (RO_REGISTRATION_COOKIE)0;
            }
            finally
            {
                IsDisposed = true;
            }
        }
    }

    /// <summary>
    /// Occurs when the server has no live objects.
    /// </summary>
    public event EventHandler? Empty;

    /// <summary>
    /// Occurs when the server creates an object.
    /// </summary>
    public event EventHandler<InstanceCreatedEventArgs>? InstanceCreated;
}
