// SPDX-FileCopyrightText: 2026 Anton Shirokov
// SPDX-License-Identifier: MIT

namespace RetroSound.Core.Playback;

/// <summary>
/// Exposes runtime control over whether a tick player should loop when it reaches the end of its musical data.
/// </summary>
public interface ILoopingPlaybackController
{
    /// <summary>
    /// Gets a value indicating whether the player can loop its current stream.
    /// </summary>
    bool SupportsLooping { get; }

    /// <summary>
    /// Gets or sets a value indicating whether looping is enabled.
    /// </summary>
    bool IsLoopingEnabled { get; set; }
}
