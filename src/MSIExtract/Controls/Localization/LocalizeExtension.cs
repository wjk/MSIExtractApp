// Copyright (c) William Kent. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Markup;
using System.Xaml;

namespace MSIExtract.Controls.Localization
{
    /// <summary>
    /// A <see cref="MarkupExtension"/> that looks a string up from a <see cref="PRIResourceLoader"/>.
    /// </summary>
    public sealed class LocalizeExtension : MarkupExtension
    {
        /// <summary>
        /// Gets or sets the resource key to use. If unset, this will be automatically derived from a
        /// combination of the target object's <see cref="UIElement.Uid"/> and the name
        /// of the property the receiver is providing a value for.
        /// </summary>
        public string? Key { get; set; } = null;

        /// <summary>
        /// Gets or sets the <see cref="PRIResourceLoader"/> instance to use. If unset, this's instance's
        /// properties will be automatically derived using the root type (to locate the assembly) and its
        /// unqualified name as the <see cref="PRIResourceLoader.ResourceMap"/>.
        /// </summary>
        public PRIResourceLoader? Loader { get; set; } = null;

        /// <inheritdoc/>
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            ArgumentNullException.ThrowIfNull(serviceProvider);

            if (Loader == null)
            {
                IRootObjectProvider? rootObjectProvider = (IRootObjectProvider?)serviceProvider.GetService(typeof(IRootObjectProvider));
                if (rootObjectProvider == null)
                {
                    throw new InvalidOperationException("Loader is not set and IRootObjectProvider could not be obtained");
                }

                Type type = rootObjectProvider.RootObject.GetType();
                Loader = new PRIResourceLoader(type, type.Name);
            }

            string? key = this.Key;
            if (key == null)
            {
                IProvideValueTarget? valueTarget = (IProvideValueTarget?)serviceProvider.GetService(typeof(IProvideValueTarget));
                if (valueTarget == null)
                {
                    throw new InvalidOperationException("Could not get IProvideValueTarget");
                }

                if (valueTarget.TargetObject is UIElement element)
                {
                    if (string.IsNullOrEmpty(element.Uid))
                    {
                        throw new InvalidOperationException("x:Uid must be set to use automatic key lookup");
                    }

                    string propertyName = valueTarget.TargetProperty switch
                    {
                        DependencyProperty prop => prop.Name,
                        System.Reflection.PropertyInfo prop => prop.Name,
                        _ => throw new InvalidOperationException("Unexpected TargetProperty type")
                    };

                    key = element.Uid + "." + propertyName;
                }
            }

            if (key == null)
            {
                throw new InvalidOperationException("Key not specified and could not be looked up automatically");
            }

            return Loader.GetString(key);
        }
    }
}
