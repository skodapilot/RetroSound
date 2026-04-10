// SPDX-FileCopyrightText: 2026 Anton Shirokov
// SPDX-License-Identifier: MIT

namespace RetroSound.Core.Playback.Tracker;

/// <summary>
/// Represents one tracker row for a single AY/YM chip stream.
/// </summary>
internal sealed class SingleChipPatternRow
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SingleChipPatternRow"/> class.
    /// </summary>
    /// <param name="tickDuration">The number of 50 Hz ticks this row lasts.</param>
    /// <param name="channelA">The command for channel A.</param>
    /// <param name="channelB">The command for channel B.</param>
    /// <param name="channelC">The command for channel C.</param>
    /// <param name="noisePeriod">The optional noise period written when the row starts.</param>
    /// <param name="envelopePeriod">The optional envelope period written when the row starts.</param>
    /// <param name="envelopeShape">The optional envelope shape written when the row starts.</param>
    public SingleChipPatternRow(
        int tickDuration,
        SingleChipChannelCommand? channelA = null,
        SingleChipChannelCommand? channelB = null,
        SingleChipChannelCommand? channelC = null,
        int? noisePeriod = null,
        int? envelopePeriod = null,
        int? envelopeShape = null)
    {
        if (tickDuration <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tickDuration), "Tick duration must be greater than zero.");
        }

        if (noisePeriod is < 0 or > 0x1F)
        {
            throw new ArgumentOutOfRangeException(nameof(noisePeriod), "Noise period must be between 0 and 31.");
        }

        if (envelopePeriod is < 0 or > 0xFFFF)
        {
            throw new ArgumentOutOfRangeException(nameof(envelopePeriod), "Envelope period must be between 0 and 65535.");
        }

        if (envelopeShape is < 0 or > 0xFF)
        {
            throw new ArgumentOutOfRangeException(nameof(envelopeShape), "Envelope shape must be between 0 and 255.");
        }

        TickDuration = tickDuration;
        ChannelA = channelA ?? new SingleChipChannelCommand();
        ChannelB = channelB ?? new SingleChipChannelCommand();
        ChannelC = channelC ?? new SingleChipChannelCommand();
        NoisePeriod = noisePeriod;
        EnvelopePeriod = envelopePeriod;
        EnvelopeShape = envelopeShape;
    }

    /// <summary>
    /// Gets the number of logical ticks this row lasts.
    /// </summary>
    public int TickDuration { get; }

    /// <summary>
    /// Gets the row command for channel A.
    /// </summary>
    public SingleChipChannelCommand ChannelA { get; }

    /// <summary>
    /// Gets the row command for channel B.
    /// </summary>
    public SingleChipChannelCommand ChannelB { get; }

    /// <summary>
    /// Gets the row command for channel C.
    /// </summary>
    public SingleChipChannelCommand ChannelC { get; }

    /// <summary>
    /// Gets the optional noise period written when the row starts.
    /// </summary>
    public int? NoisePeriod { get; }

    /// <summary>
    /// Gets the optional envelope period written when the row starts.
    /// </summary>
    public int? EnvelopePeriod { get; }

    /// <summary>
    /// Gets the optional envelope shape written when the row starts.
    /// </summary>
    public int? EnvelopeShape { get; }
}
