// SPDX-FileCopyrightText: 2026 Anton Shirokov
// SPDX-License-Identifier: MIT

namespace RetroSound.Core.Playback.Tracker.Internal;

internal sealed class TrackerPlaybackCursor(SingleChipTrackerModule module)
{

    private int OrderIndex { get; set; }

    private int RowIndex { get; set; }

    private int TickIndexInRow { get; set; }

    public bool IsEndOfStream { get; private set; }

    public bool IsAtRowStart => TickIndexInRow == 0;

    public SingleChipPatternRow CurrentPatternRow
    {
        get
        {
            var patternIndex = module.Order[OrderIndex];
            return module.Patterns[patternIndex].Rows[RowIndex];
        }
    }

    public void Reset()
    {
        OrderIndex = 0;
        RowIndex = 0;
        TickIndexInRow = 0;
        IsEndOfStream = false;
    }

    public void Advance()
    {
        if (IsEndOfStream)
        {
            return;
        }

        TickIndexInRow++;
        if (TickIndexInRow < CurrentPatternRow.TickDuration)
        {
            return;
        }

        TickIndexInRow = 0;

        var patternIndex = module.Order[OrderIndex];
        var pattern = module.Patterns[patternIndex];
        RowIndex++;
        if (RowIndex < pattern.Rows.Count)
        {
            return;
        }

        RowIndex = 0;
        OrderIndex++;
        if (OrderIndex < module.Order.Count)
        {
            return;
        }

        IsEndOfStream = true;
    }
}