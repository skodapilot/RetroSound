// SPDX-FileCopyrightText: 2026 Anton Shirokov
// SPDX-License-Identifier: MIT

namespace RetroSound.Core.Playback.Tracker;

/// <summary>
/// Describes the per-tick effect that can be applied to a channel after a row is emitted.
/// </summary>
internal enum SingleChipChannelEffectType
{
    /// <summary>
    /// No per-tick effect is active.
    /// </summary>
    None = 0,

    /// <summary>
    /// Adjusts the tone period by a signed delta after each emitted tick.
    /// </summary>
    ToneSlide = 1,

    /// <summary>
    /// Adjusts the channel volume by a signed delta after each emitted tick.
    /// </summary>
    VolumeSlide = 2,
}
