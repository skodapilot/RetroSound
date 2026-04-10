// SPDX-FileCopyrightText: 2026 Anton Shirokov
// SPDX-License-Identifier: MIT

namespace RetroSound.Core.Formats.Pt3;

/// <summary>
/// Represents errors that occur when a PT3 module is invalid, truncated, or unsupported.
/// </summary>
public sealed class Pt3FormatException : FormatException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Pt3FormatException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    public Pt3FormatException(string message)
        : base(message)
    {
    }
}