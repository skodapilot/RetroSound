// SPDX-FileCopyrightText: 2026 Anton Shirokov
// SPDX-License-Identifier: MIT

namespace RetroSound.Core.Formats.Pt3;

/// <summary>
/// Represents one PT3 pattern entry and its three channel streams.
/// </summary>
public sealed class Pt3Pattern
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Pt3Pattern"/> class.
    /// </summary>
    /// <param name="index">The zero-based pattern index.</param>
    /// <param name="channelA">The pattern bytecode stream for channel A.</param>
    /// <param name="channelB">The pattern bytecode stream for channel B.</param>
    /// <param name="channelC">The pattern bytecode stream for channel C.</param>
    public Pt3Pattern(
        int index,
        Pt3ChannelPattern channelA,
        Pt3ChannelPattern channelB,
        Pt3ChannelPattern channelC)
    {
        if (index < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(index), "The pattern index cannot be negative.");
        }

        ArgumentNullException.ThrowIfNull(channelA);
        ArgumentNullException.ThrowIfNull(channelB);
        ArgumentNullException.ThrowIfNull(channelC);

        Index = index;
        ChannelA = channelA;
        ChannelB = channelB;
        ChannelC = channelC;
    }

    /// <summary>
    /// Gets the zero-based pattern index.
    /// </summary>
    public int Index { get; }

    /// <summary>
    /// Gets the channel A bytecode stream.
    /// </summary>
    public Pt3ChannelPattern ChannelA { get; }

    /// <summary>
    /// Gets the channel B bytecode stream.
    /// </summary>
    public Pt3ChannelPattern ChannelB { get; }

    /// <summary>
    /// Gets the channel C bytecode stream.
    /// </summary>
    public Pt3ChannelPattern ChannelC { get; }
}