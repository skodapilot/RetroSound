// SPDX-FileCopyrightText: 2026 Anton Shirokov
// SPDX-License-Identifier: MIT

namespace RetroSound.Core.Models;

/// <summary>
/// Describes the static configuration for an AY/YM emulator instance.
/// </summary>
public sealed class AyYmChipConfiguration
{
    /// <summary>
    /// The default AY/YM chip clock used when no explicit value is supplied.
    /// </summary>
    public const int DefaultChipClockHz = 1_773_400;

    /// <summary>
    /// Initializes a new instance of the <see cref="AyYmChipConfiguration"/> class.
    /// </summary>
    /// <param name="chipClockHz">The AY/YM chip clock in Hertz.</param>
    /// <param name="chipType">The AY/YM chip family that selects the DAC model.</param>
    /// <param name="outputChannelCount">
    /// The number of interleaved PCM channels expected from the emulator. Use <c>1</c> for mono or <c>2</c> for
    /// stereo left/right output.
    /// </param>
    public AyYmChipConfiguration(
        int chipClockHz = DefaultChipClockHz,
        AyYmChipType chipType = AyYmChipType.Ym2149,
        int outputChannelCount = 2)
    {
        if (chipClockHz <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(chipClockHz), "Chip clock must be greater than zero.");
        }

        if (outputChannelCount is < 1 or > 2)
        {
            throw new ArgumentOutOfRangeException(
                nameof(outputChannelCount),
                "Only mono and stereo AY/YM output are currently supported.");
        }

        ChipClockHz = chipClockHz;
        ChipType = chipType;
        OutputChannelCount = outputChannelCount;
    }

    /// <summary>
    /// Gets the AY/YM chip clock in Hertz.
    /// </summary>
    public int ChipClockHz { get; }

    /// <summary>
    /// Gets the AY/YM chip family that selects the DAC curve.
    /// </summary>
    public AyYmChipType ChipType { get; }

    /// <summary>
    /// Gets the number of interleaved PCM channels expected from the emulator.
    /// </summary>
    public int OutputChannelCount { get; }
}