// SPDX-FileCopyrightText: 2026 Anton Shirokov
// SPDX-License-Identifier: MIT

namespace RetroSound.Core.Models;

/// <summary>
/// Describes the relationship between logical playback ticks and rendered audio samples.
/// </summary>
public readonly record struct PlaybackTiming
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PlaybackTiming"/> struct.
    /// </summary>
    /// <param name="ticksPerSecond">The logical tracker tick rate.</param>
    /// <param name="sampleRate">The PCM output sample rate.</param>
    public PlaybackTiming(double ticksPerSecond, int sampleRate)
    {
        if (ticksPerSecond <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(ticksPerSecond), "Tick rate must be greater than zero.");
        }

        if (sampleRate <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sampleRate), "Sample rate must be greater than zero.");
        }

        TicksPerSecond = ticksPerSecond;
        SampleRate = sampleRate;
    }

    /// <summary>
    /// Gets the logical tracker tick rate.
    /// </summary>
    public double TicksPerSecond { get; }

    /// <summary>
    /// Gets the PCM output sample rate.
    /// </summary>
    public int SampleRate { get; }

    /// <summary>
    /// Gets the fractional number of PCM sample frames that correspond to one logical tick.
    /// </summary>
    public double SamplesPerTick => SampleRate / TicksPerSecond;

    /// <summary>
    /// Gets the deterministic number of PCM sample frames rendered for each logical tick.
    /// </summary>
    /// <remarks>
    /// The current playback pipeline renders one whole audio block per 50 Hz music tick. Rounding once through this
    /// helper keeps every player, emulator, and output adapter aligned to the same per-tick sample count.
    /// </remarks>
    /// <returns>The rounded sample frame count for one logical tick.</returns>
    public int GetSampleFramesPerTick()
    {
        return (int)Math.Round(SamplesPerTick, MidpointRounding.AwayFromZero);
    }
}