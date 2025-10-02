using System;
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
using static Windows.Win32.PInvoke;

namespace Shmuelie.WinRTServer;

/// <summary>
/// An Out of Process COM Server.
/// </summary>
/// <remarks>
/// <para>Allows for types to be created using COM activation instead of WinRT activation like <see cref="WinRtServer"/>.</para>
/// <para>Typical usage is to call from an <see langword="await"/> <see langword="using"/> block, using <see cref="WaitForRunDown"/> to not close until it is safe to do so.</para>
/// <code language="cs">
/// <![CDATA[
/// await using (ComServer server = new ComServer())
/// {
///     server.RegisterClass<RemoteThing, IRemoteThing>();
///     server.Start();
///     await server.WaitForRunDown();
/// }
/// ]]>
/// </code>
/// </remarks>
/// <see cref="IAsyncDisposable"/>
/// <threadsafety static="true" instance="false"/>
[SupportedOSPlatform("windows6.0.6000")]
public sealed class ComServer : IAsyncDisposable
{
    /// <summary>
    /// Map of class factories and the registration cookie from the CLSID that the factory creates.
    /// </summary>
    private readonly Dictionary<Guid, (BaseClassFactory factory, uint cookie)> factories = [];

    /// <summary>
    /// Collection of created instances.
    /// </summary>
    private readonly List<WeakReference> liveServers = new();

    /// <summary>
    /// Timer that checks if all created instances have been collected.
    /// </summary>
    private readonly System.Timers.Timer lifetimeCheckTimer;

    /// <summary>
    /// Tracks the creation of the first instance after server is started.
    /// </summary>
    private TaskCompletionSource<object>? firstInstanceCreated;

    /// <summary>
    /// Initializes a new instance of the <see cref="ComServer"/> class.
    /// </summary>
    public unsafe ComServer()
    {
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

        int? instanceCount = null;
        GC.Collect();
        GC.WaitForPendingFinalizers();
        for (int i = liveServers.Count - 1; i >= 0; i--)
        {
            WeakReference weakRef = liveServers[i];
            if (!weakRef.IsAlive)
            {
                liveServers.RemoveAt(i);
                instanceCount = liveServers.Count;
            }
        }

        if (instanceCount == 0)
        {
            Empty?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Register a class factory with the server.
    /// </summary>
    /// <param name="factory">The class factory to register.</param>
    /// <param name="comWrappers">The implementation of <see cref="ComWrappers"/> to use for wrapping.</param>
    /// <returns><see langword="true"/> if <paramref name="factory"/> was registered; otherwise, <see langword="false"/>.</returns>
    /// <remarks>Only one factory can be registered for a CLSID.</remarks>
    /// <exception cref="ObjectDisposedException">The instance is disposed.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="factory"/> or <paramref name="comWrappers"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">The server is running.</exception>
    /// <seealso cref="UnregisterClassFactory(Guid)"/>
    public unsafe bool RegisterClassFactory(BaseClassFactory factory, ComWrappers comWrappers)
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        ArgumentNullException.ThrowIfNull(factory);
        ArgumentNullException.ThrowIfNull(comWrappers);
        if (lifetimeCheckTimer.Enabled)
        {
            throw new InvalidOperationException("Can only add class factories when server is not running.");
        }

        Guid clsid = factory.Clsid;

        if (factories.ContainsKey(clsid))
        {
            return false;
        }

        factory.InstanceCreated += Factory_InstanceCreated;

        nint wrapper = comWrappers.GetOrCreateComInterfaceForObject(new BaseClassFactoryWrapper(factory, comWrappers), CreateComInterfaceFlags.None);

        uint cookie;
        CoRegisterClassObject(&clsid, (IUnknown*)wrapper, CLSCTX.CLSCTX_LOCAL_SERVER, (REGCLS.REGCLS_MULTIPLEUSE | REGCLS.REGCLS_SUSPENDED), &cookie).ThrowOnFailure();

        factories.Add(clsid, (factory, cookie));
        return true;
    }

    /// <summary>
    /// Unregister a class factory.
    /// </summary>
    /// <param name="clsid">The CLSID of the server to remove.</param>
    /// <returns><see langword="true"/> if the server was removed; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ObjectDisposedException">The instance is disposed.</exception>
    /// <exception cref="InvalidOperationException">The server is running.</exception>
    /// <seealso cref="RegisterClassFactory(BaseClassFactory, ComWrappers)"/>
    public unsafe bool UnregisterClassFactory(Guid clsid)
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        if (lifetimeCheckTimer.Enabled)
        {
            throw new InvalidOperationException("Can only remove class factories when server is not running.");
        }

        if (!factories.TryGetValue(clsid, out (BaseClassFactory factory, uint cookie) data))
        {
            return false;
        }
        factories.Remove(clsid);

        data.factory.InstanceCreated -= Factory_InstanceCreated;

        CoRevokeClassObject(data.cookie).ThrowOnFailure();
        return true;
    }

    private void Factory_InstanceCreated(object? sender, InstanceCreatedEventArgs e)
    {
        if (IsDisposed)
        {
            return;
        }

        liveServers.Add(new WeakReference(e.Instance));
        InstanceCreated?.Invoke(this, e);
        firstInstanceCreated?.TrySetResult(e.Instance);

        // Reset the TaskCompletionSource to avoid keeping a reference to the first instance created indefinitely.
        firstInstanceCreated = new TaskCompletionSource<object>();
    }

    /// <summary>
    /// Gets a value indicating whether the server is running.
    /// </summary>
    public bool IsRunning => lifetimeCheckTimer.Enabled;

    /// <summary>
    /// Starts the server.
    /// </summary>
    /// <remarks>Calling <see cref="Start"/> is non-blocking.</remarks>
    /// <exception cref="ObjectDisposedException">The instance is disposed.</exception>
    public void Start()
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        if (lifetimeCheckTimer.Enabled)
        {
            return;
        }

        firstInstanceCreated = new();
        lifetimeCheckTimer.Start();
        CoResumeClassObjects().ThrowOnFailure();
    }

    /// <summary>
    /// Stops the server.
    /// </summary>
    /// <exception cref="ObjectDisposedException">The instance is disposed.</exception>
    public void Stop()
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        if (!lifetimeCheckTimer.Enabled)
        {
            return;
        }

        firstInstanceCreated = null;
        lifetimeCheckTimer.Stop();
        CoSuspendClassObjects().ThrowOnFailure();
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
        if (!lifetimeCheckTimer.Enabled)
        {
            throw new InvalidOperationException("ComServer must be started before you can call WaitForRunDown()");
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
    /// Gets a value indicating whether the instance is disposed.
    /// </summary>
    public bool IsDisposed
    {
        get;
        private set;
    }

    /// <summary>
    /// Force the server to stop and release all resources.
    /// </summary>
    /// <remarks>Unlike <see cref="DisposeAsync"/>, <see cref="UnsafeDispose"/> will ignore if any objects are still alive before unregistering class factories.</remarks>
    /// <seealso cref="DisposeAsync"/>
    public void UnsafeDispose()
    {
        if (!IsDisposed)
        {
            try
            {
                _ = CoSuspendClassObjects();

                liveServers.Clear();
                lifetimeCheckTimer.Stop();
                lifetimeCheckTimer.Dispose();

                foreach (var clsid in factories.Keys)
                {
                    _ = UnregisterClassFactory(clsid);
                }
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
                _ = CoSuspendClassObjects();

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

                foreach (var clsid in factories.Keys)
                {
                    _ = UnregisterClassFactory(clsid);
                }
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
