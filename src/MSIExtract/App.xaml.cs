// Copyright (c) William Kent. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Runtime.InteropServices.Marshalling;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using KPreisser.UI;
using MSIExtract.ShellExtension;
using MSIExtract.Views;
using PresentationTheme.Aero;
using Shmuelie.WinRTServer;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;

namespace MSIExtract
{
    /// <summary>
    /// The main application subclass.
    /// </summary>
    public partial class App
    {
        /// <inheritdoc/>
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            if (e.Args.Contains("/COMServer", StringComparer.OrdinalIgnoreCase))
            {
                RunCOMServer();
                return;
            }

            AeroTheme.SetAsCurrentTheme();

            IActivatedEventArgs? activationArgs = null;

            try
            {
                activationArgs = AppInstance.GetActivatedEventArgs();
            }
            catch (System.Runtime.InteropServices.COMException)
            {
                activationArgs = null;
            }

            if (activationArgs != null)
            {
                switch (activationArgs.Kind)
                {
                    case ActivationKind.Launch:
                        this.MainWindow = new MainWindow();
                        break;

                    case ActivationKind.File:
                        var fileArgs = (FileActivatedEventArgs)activationArgs;

                        if (fileArgs.Files.Count > 0)
                        {
                            this.MainWindow = new MainWindow(fileArgs.Files[0].Path);
                        }
                        else
                        {
                            this.MainWindow = new MainWindow();
                        }

                        break;

                    default:
                        this.MainWindow = new MainWindow();
                        break;
                }
            }
            else
            {
                if (e != null && e.Args.Length > 0)
                {
                    this.MainWindow = new MainWindow(e.Args[0]);
                }
                else
                {
                    this.MainWindow = new MainWindow();
                }
            }

            this.MainWindow.Show();
        }

        private void RunCOMServer()
        {
            async Task RunCOMServerTask()
            {
                await using (ComServer server = new ComServer())
                {
                    StrategyBasedComWrappers wrappers = new StrategyBasedComWrappers();

                    server.RegisterClass<MSIViewerOpenCommand, ShellCommandLib.Interop.IExplorerCommand>(wrappers);
                    server.Empty += ComServer_Empty;
                    server.Start();
                    await server.WaitForRunDown();
                }
            }

            void ThreadEntry(object? parameter)
            {
                RunCOMServerTask().GetAwaiter().GetResult();
            }

            Thread thread = new Thread(ThreadEntry);
            thread.SetApartmentState(ApartmentState.MTA);
            thread.Start();
        }

        private void ComServer_Empty(object? sender, EventArgs e)
        {
            Dispatcher.Invoke(Shutdown);
        }
    }
}
