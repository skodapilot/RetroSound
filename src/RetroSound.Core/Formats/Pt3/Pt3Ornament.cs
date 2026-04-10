// SPDX-FileCopyrightText: 2026 Anton Shirokov
// SPDX-License-Identifier: MIT

namespace RetroSound.Core.Formats.Pt3;

/// <summary>
/// Represents one PT3 ornament definition.
/// </summary>
public sealed class Pt3Ornament
{
    private readonly IReadOnlyList<sbyte> _toneOffsets;

    /// <summary>
    /// Initializes a new instance of the <see cref="Pt3Ornament"/> class.
    /// </summary>
    /// <param name="index">The ornament index in the PT3 ornament table.</param>
    /// <param name="dataOffset">The source module offset of the ornament definition.</param>
    /// <param name="loopPosition">The zero-based ornament step index used when looping.</param>
    /// <param name="toneOffsets">The ornament semitone offsets.</param>
    public Pt3Ornament(int index, int dataOffset, int loopPosition, IEnumerable<sbyte> toneOffsets)
    {
        if (index < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(index), "The ornament index cannot be negative.");
        }

        if (dataOffset < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(dataOffset), "The ornament offset cannot be negative.");
        }

        ArgumentNullException.ThrowIfNull(toneOffsets);

        var offsetArray = toneOffsets.ToArray();
        if (offsetArray.Length == 0)
        {
            throw new ArgumentException("A PT3 ornament must contain at least one step.", nameof(toneOffsets));
        }

        if (loopPosition < 0 || loopPosition >= offsetArray.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(loopPosition), "The ornament loop position must point to an existing step.");
        }

        Index = index;
        DataOffset = dataOffset;
        LoopPosition = loopPosition;
        _toneOffsets = Array.AsReadOnly(offsetArray);
    }

    /// <summary>
    /// Gets the ornament index in the PT3 ornament table.
    /// </summary>
    public int Index { get; }

    /// <summary>
    /// Gets the source module offset of the ornament definition.
    /// </summary>
    public int DataOffset { get; }

    /// <summary>
    /// Gets the zero-based ornament step index used when looping.
    /// </summary>
    public int LoopPosition { get; }

    /// <summary>
    /// Gets the ornament semitone offsets.
    /// </summary>
    public IReadOnlyList<sbyte> ToneOffsets => _toneOffsets;
}