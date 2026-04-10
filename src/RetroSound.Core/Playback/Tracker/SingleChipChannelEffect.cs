// SPDX-FileCopyrightText: 2026 Anton Shirokov
// SPDX-License-Identifier: MIT

namespace RetroSound.Core.Playback.Tracker;

/// <summary>
/// Describes a simple per-tick effect for one channel.
/// </summary>
internal readonly record struct SingleChipChannelEffect
{
    /// <summary>
    /// Gets an effect value that leaves the channel unchanged between ticks.
    /// </summary>
    public static SingleChipChannelEffect None => new(SingleChipChannelEffectType.None, 0);

    /// <summary>
    /// Initializes a new instance of the <see cref="SingleChipChannelEffect"/> struct.
    /// </summary>
    /// <param name="type">The effect type to apply between ticks.</param>
    /// <param name="delta">The signed delta applied by the effect on each tick.</param>
    public SingleChipChannelEffect(SingleChipChannelEffectType type, int delta)
    {
        Type = type;
        Delta = delta;
    }

    /// <summary>
    /// Gets the effect type.
    /// </summary>
    public SingleChipChannelEffectType Type { get; }

    /// <summary>
    /// Gets the signed delta applied by the effect.
    /// </summary>
    public int Delta { get; }
}
