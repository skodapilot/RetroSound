// SPDX-FileCopyrightText: 2026 Anton Shirokov
// SPDX-License-Identifier: MIT

namespace RetroSound.Core.Formats.TurboSound;

/// <summary>
/// Represents an error while parsing a TurboSound container.
/// </summary>
public sealed class TurboSoundContainerFormatException : FormatException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TurboSoundContainerFormatException"/> class.
    /// </summary>
    /// <param name="message">The parse error message.</param>
    public TurboSoundContainerFormatException(string message)
        : base(message)
    {
    }
}