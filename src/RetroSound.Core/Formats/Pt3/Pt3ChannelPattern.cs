// SPDX-FileCopyrightText: 2026 Anton Shirokov
// SPDX-License-Identifier: MIT

namespace RetroSound.Core.Formats.Pt3;

/// <summary>
/// Represents one PT3 channel bytecode stream inside a pattern.
/// </summary>
public sealed class Pt3ChannelPattern
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Pt3ChannelPattern"/> class.
    /// </summary>
    /// <param name="name">The logical channel name.</param>
    /// <param name="dataOffset">The offset of the channel stream inside the source module.</param>
    /// <param name="commandStream">The raw channel stream bytes, including in-stream row separators.</param>
    public Pt3ChannelPattern(string name, int dataOffset, ReadOnlyMemory<byte> commandStream)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("The channel name must not be null, empty, or whitespace.", nameof(name));
        }

        if (dataOffset < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(dataOffset), "The channel offset cannot be negative.");
        }

        Name = name;
        DataOffset = dataOffset;
        CommandStream = commandStream;
    }

    /// <summary>
    /// Gets the logical channel name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the offset of the channel stream inside the source module.
    /// </summary>
    public int DataOffset { get; }

    /// <summary>
    /// Gets the raw channel stream bytes, including in-stream row separators.
    /// </summary>
    public ReadOnlyMemory<byte> CommandStream { get; }
}