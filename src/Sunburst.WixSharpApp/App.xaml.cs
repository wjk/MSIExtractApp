// Copyright (c) William Kent. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using KPreisser.UI;
using Windows.ApplicationModel;

namespace Sunburst.WixSharpApp
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
                    Title = "WiX Sharp",
                    Instruction = "WiX Sharp cannot be run outside of its Windows Store package.",
                    Icon = TaskDialogIcon.Get(TaskDialogStandardIcon.Error),
                };

                page.StandardButtons.Add(TaskDialogResult.Close);
                var td = new TaskDialog(page);
                td.Show();

                this.Shutdown();
            }

            this.MainWindow = new MainWindow();
            this.MainWindow.Show();
        }

        private static bool CheckPackageIdentity()
        {
            try
            {
                const string expectedPFN = "Sunburst.WixSharpApp_pe4vsf0t72evj";
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
