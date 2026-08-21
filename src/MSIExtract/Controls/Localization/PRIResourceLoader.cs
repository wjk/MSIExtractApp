// Copyright (c) William Kent. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using Microsoft.ApplicationModel.Resources;

namespace MSIExtract.Controls.Localization;

/// <summary>
/// Provides a way to load string and binary resources from a *.pri file.
/// </summary>
/// <remarks>
/// The *.pri file used will be located using the path to the assembly containing the
/// <see cref="TypeInTargetAssembly"/>. If the assembly is named <c>MyAssembly.dll</c>,
/// the file <c>MyAssembly.pri</c> in the same directory will be queried.
/// </remarks>
public class PRIResourceLoader
{
    private ResourceManager? resourceManager;
    private ResourceMap? resourceLoader;

    /// <summary>
    /// Initializes a new instance of the <see cref="PRIResourceLoader"/> class with no properties set.
    /// </summary>
    /// <remarks>
    /// Using <see cref="PRIResourceLoader"/> in a XAML resource dictionary requires
    /// that there be a public parameterless constructor.
    /// </remarks>
    public PRIResourceLoader()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PRIResourceLoader"/> class.
    /// </summary>
    /// <param name="typeInTargetAssembly">
    /// A type in the assembly that owns the *.pri file to use.
    /// </param>
    /// <param name="resourceMap">
    /// A string designating the submap of the *.pri file to reference.
    /// </param>
    public PRIResourceLoader(Type typeInTargetAssembly, string resourceMap)
    {
        this.TypeInTargetAssembly = typeInTargetAssembly;
        this.ResourceMap = resourceMap;
    }

    /// <summary>
    /// Gets or sets a type in the assembly that owns the *.pri file to use.
    /// </summary>
    public Type? TypeInTargetAssembly { get; set; } = null;

    /// <summary>
    /// Gets or sets a string designating the submap of the *.pri file to reference.
    /// </summary>
    public string ResourceMap { get; set; } = string.Empty;

    /// <summary>
    /// Looks up a string. An exception will be thrown if the <paramref name="key"/> is not found.
    /// </summary>
    /// <param name="key">
    /// The key to look up in the <see cref="ResourceMap"/>.
    /// </param>
    /// <returns>
    /// A <see cref="string"/>, or a placeholder if the *.pri file could not be loaded
    /// (for WPF designer support).
    /// </returns>
    public string GetString(string key)
    {
        if (this.resourceLoader == null)
        {
            this.LoadResources();
        }

        if (this.resourceLoader == null)
        {
            // If the resource loader is still null after being loaded, that means that the
            // PRI file could not be found. Return a fallback in that case.
            return "{" + key + "}";
        }

        ResourceCandidate candidate = this.resourceLoader.GetValue(key);
        return candidate.ValueAsString;
    }

    /// <summary>
    /// Looks up an embedded binary file as a <see cref="Stream"/>. An exception will be thrown
    /// if the <paramref name="key"/> is not found.
    /// </summary>
    /// <param name="key">
    /// The key to look up in the <see cref="ResourceMap"/>.
    /// </param>
    /// <returns>
    /// A <see cref="Stream"/> instance, or <c>null</c> if the *.pri file could not be loaded.
    /// </returns>
    public Stream? GetStream(string key)
    {
        if (this.resourceLoader == null)
        {
            this.LoadResources();
        }

        if (this.resourceLoader == null)
        {
            return null;
        }

        ResourceCandidate candidate = this.resourceLoader.GetValue(key);
        return new MemoryStream(candidate.ValueAsBytes);
    }

    private void LoadResources()
    {
        if (TypeInTargetAssembly == null)
        {
            return;
        }

        if (ResourceMap == null)
        {
            return;
        }

        string path = Path.ChangeExtension(this.TypeInTargetAssembly.Assembly.Location, ".pri");
        if (!File.Exists(path))
        {
            return;
        }

        this.resourceManager = new ResourceManager(path);
        this.resourceLoader = this.resourceManager.MainResourceMap.GetSubtree(ResourceMap);
    }
}
