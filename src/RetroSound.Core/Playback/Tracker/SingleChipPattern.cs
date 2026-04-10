// SPDX-FileCopyrightText: 2026 Anton Shirokov
// SPDX-License-Identifier: MIT

namespace RetroSound.Core.Playback.Tracker;

/// <summary>
/// Represents one reusable pattern referenced by the module order list.
/// </summary>
internal sealed class SingleChipPattern
{

    /// <summary>
    /// Initializes a new instance of the <see cref="SingleChipPattern"/> class.
    /// </summary>
    /// <param name="rows">The rows stored in the pattern.</param>
    public SingleChipPattern(IEnumerable<SingleChipPatternRow> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);

        var rowArray = rows.ToArray();
        if (rowArray.Length == 0)
        {
            throw new ArgumentException("A pattern must contain at least one row.", nameof(rows));
        }

        Rows = Array.AsReadOnly(rowArray);
    }

    /// <summary>
    /// Gets the rows stored in the pattern.
    /// </summary>
    public IReadOnlyList<SingleChipPatternRow> Rows { get; }
}
