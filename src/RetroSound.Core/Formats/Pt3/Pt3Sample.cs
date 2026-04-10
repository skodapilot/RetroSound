// SPDX-FileCopyrightText: 2026 Anton Shirokov
// SPDX-License-Identifier: MIT

namespace RetroSound.Core.Formats.Pt3;

/// <summary>
/// Represents one PT3 sample definition.
/// </summary>
public sealed class Pt3Sample
{
    private readonly IReadOnlyList<Pt3SampleStep> _steps;

    /// <summary>
    /// Initializes a new instance of the <see cref="Pt3Sample"/> class.
    /// </summary>
    /// <param name="index">The sample index in the PT3 sample table.</param>
    /// <param name="dataOffset">The source module offset of the sample definition.</param>
    /// <param name="loopPosition">The zero-based step index used when looping the sample.</param>
    /// <param name="steps">The decoded sample steps.</param>
    public Pt3Sample(int index, int dataOffset, int loopPosition, IEnumerable<Pt3SampleStep> steps)
    {
        if (index < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(index), "The sample index cannot be negative.");
        }

        if (dataOffset < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(dataOffset), "The sample offset cannot be negative.");
        }

        ArgumentNullException.ThrowIfNull(steps);

        var stepArray = steps.ToArray();
        if (stepArray.Length == 0)
        {
            throw new ArgumentException("A PT3 sample must contain at least one step.", nameof(steps));
        }

        if (loopPosition < 0 || loopPosition >= stepArray.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(loopPosition), "The sample loop position must point to an existing step.");
        }

        Index = index;
        DataOffset = dataOffset;
        LoopPosition = loopPosition;
        _steps = Array.AsReadOnly(stepArray);
    }

    /// <summary>
    /// Gets the sample index in the PT3 sample table.
    /// </summary>
    public int Index { get; }

    /// <summary>
    /// Gets the source module offset of the sample definition.
    /// </summary>
    public int DataOffset { get; }

    /// <summary>
    /// Gets the zero-based step index used when looping the sample.
    /// </summary>
    public int LoopPosition { get; }

    /// <summary>
    /// Gets the decoded sample steps.
    /// </summary>
    public IReadOnlyList<Pt3SampleStep> Steps => _steps;
}