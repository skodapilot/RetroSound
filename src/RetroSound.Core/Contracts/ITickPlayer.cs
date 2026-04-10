// SPDX-FileCopyrightText: 2026 Anton Shirokov
// SPDX-License-Identifier: MIT

using RetroSound.Core.Models;
namespace RetroSound.Core.Playback;

/// <summary>
/// Produces complete AY/YM register frames one logical playback tick at a time.
/// </summary>
public interface ITickPlayer
{
    /// <summary>
    /// Gets the playback timing used by the player.
    /// </summary>
    PlaybackTiming Timing { get; }

    /// <summary>
    /// Gets a value indicating whether the player has reached the end of its stream.
    /// </summary>
    bool IsEndOfStream { get; }

    /// <summary>
    /// Resets the player back to its initial playback state.
    /// </summary>
    void Reset();

    /// <summary>
    /// Advances playback by one logical tick.
    /// </summary>
    /// <param name="frame">When this method returns <see langword="true"/>, contains the next AY/YM register frame.</param>
    /// <returns><see langword="true"/> when a frame was produced; otherwise, <see langword="false"/>.</returns>
    bool TryAdvance(out AyRegisterFrame frame);
}
