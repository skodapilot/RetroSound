// SPDX-FileCopyrightText: 2026 Anton Shirokov
// SPDX-License-Identifier: MIT

namespace RetroSound.Core.Models;

/// <summary>
/// Represents one logical playback tick containing the AY/YM register frames for both chips.
/// </summary>
public readonly record struct DualChipPlaybackFrame
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DualChipPlaybackFrame"/> struct.
    /// </summary>
    /// <param name="chipA">The register frame for the first AY/YM chip.</param>
    /// <param name="chipB">The register frame for the second AY/YM chip.</param>
    public DualChipPlaybackFrame(AyRegisterFrame chipA, AyRegisterFrame chipB)
    {
        ChipA = chipA ?? throw new ArgumentNullException(nameof(chipA));
        ChipB = chipB ?? throw new ArgumentNullException(nameof(chipB));
    }

    /// <summary>
    /// Gets the register frame for the first AY/YM chip.
    /// </summary>
    public AyRegisterFrame ChipA { get; }

    /// <summary>
    /// Gets the register frame for the second AY/YM chip.
    /// </summary>
    public AyRegisterFrame ChipB { get; }
}