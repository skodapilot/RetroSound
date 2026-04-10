// SPDX-FileCopyrightText: 2026 Anton Shirokov
// SPDX-License-Identifier: MIT

namespace RetroSound.Core.Playback;

/// <summary>
/// Represents playback-time PT3 errors such as unsupported bytecode commands or broken runtime references.
/// </summary>
public sealed class Pt3PlaybackException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Pt3PlaybackException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public Pt3PlaybackException(string message)
        : base(message)
    {
    }
}
