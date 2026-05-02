// Copyright (c) William Kent. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace AcrylicWindow;

/// <summary>
/// The different kinds of background a <see cref="AcrylicWindow"/>'s frame can have.
/// </summary>
public enum WindowFrameBackground
{
    /// <summary>
    /// The window frame will be displayed as a solid surface.
    /// </summary>
    Solid,

    /// <summary>
    /// The window frame will be displayed using the interface style for main windows (currently Mica).
    /// </summary>
    MainWindow,

    /// <summary>
    /// The window frame will be displayed using the interface style for supporting windows (currently Acrylic).
    /// </summary>
    SupportingWindow,
}
