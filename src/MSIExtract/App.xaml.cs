// Copyright (c) William Kent. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using KPreisser.UI;
using MSIExtract.Views;
using PresentationTheme.Aero;
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

            if (!CheckPackageIdentity())
            {
                var page = new TaskDialogPage
                {
                    AllowCancel = true,
                    Title = "MSI Viewer",
                    Instruction = "MSI Viewer cannot be run outside of its Windows Store package.",
                    Icon = TaskDialogIcon.Get(TaskDialogStandardIcon.Error),
                };

                page.StandardButtons.Add(TaskDialogResult.Close);
                var td = new TaskDialog(page);
                td.Show();

                this.Shutdown();
                return;
            }

            ThemeManager.Install();
            AeroTheme.SetAsCurrentTheme();

            IActivatedEventArgs? activationArgs = AppInstance.GetActivatedEventArgs();
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

        private static bool CheckPackageIdentity()
        {
            try
            {
                const string expectedPFN = "40885WilliamKent2015.MSIViewer_vv14yhe95nw30";
                return Package.Current.Id.FamilyName == expectedPFN;
            }
            catch (InvalidOperationException)
            {
                // Package.Current will throw if not called from within an AppX package.
                return false;
            }
        }
    }
}
