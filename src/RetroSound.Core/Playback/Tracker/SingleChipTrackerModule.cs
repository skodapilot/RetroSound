// SPDX-FileCopyrightText: 2026 Anton Shirokov
// SPDX-License-Identifier: MIT

namespace RetroSound.Core.Playback.Tracker;

/// <summary>
/// Represents one single-chip tracker stream that can be played by <see cref="SingleChipTrackerPlayer"/>.
/// </summary>
internal sealed class SingleChipTrackerModule
{

    /// <summary>
    /// Initializes a new instance of the <see cref="SingleChipTrackerModule"/> class.
    /// </summary>
    /// <param name="title">The optional module title.</param>
    /// <param name="order">The zero-based pattern indices used for playback order.</param>
    /// <param name="patterns">The patterns addressable by the order list.</param>
    public SingleChipTrackerModule(
        string? title,
        IEnumerable<int> order,
        IEnumerable<SingleChipPattern> patterns)
    {
        _ = title;
        ArgumentNullException.ThrowIfNull(order);
        ArgumentNullException.ThrowIfNull(patterns);

        var orderArray = order.ToArray();
        var patternArray = patterns.ToArray();

        if (orderArray.Length == 0)
        {
            throw new ArgumentException("A module must contain at least one order entry.", nameof(order));
        }

        if (patternArray.Length == 0)
        {
            throw new ArgumentException("A module must contain at least one pattern.", nameof(patterns));
        }

        for (var orderIndex = 0; orderIndex < orderArray.Length; orderIndex++)
        {
            var patternIndex = orderArray[orderIndex];
            if (patternIndex < 0 || patternIndex >= patternArray.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(order), $"Order entry {orderIndex} points to missing pattern {patternIndex}.");
            }
        }
        Order = Array.AsReadOnly(orderArray);
        Patterns = Array.AsReadOnly(patternArray);
    }

    /// <summary>
    /// Gets the pattern order used for playback.
    /// </summary>
    public IReadOnlyList<int> Order { get; }

    /// <summary>
    /// Gets the patterns stored by the module.
    /// </summary>
    public IReadOnlyList<SingleChipPattern> Patterns { get; }
}
