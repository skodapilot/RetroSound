// SPDX-FileCopyrightText: 2026 Anton Shirokov
// SPDX-License-Identifier: MIT

namespace RetroSound.Core.Playback.Tracker.Internal;

internal sealed class TrackerChipState
{
    public TrackerChipState()
    {
        ChannelStates =
        [
            new TrackerChannelState(),
            new TrackerChannelState(),
            new TrackerChannelState(),
        ];
    }

    public TrackerChannelState[] ChannelStates { get; }

    public int NoisePeriod { get; set; }

    public int EnvelopePeriod { get; set; }

    public int EnvelopeShape { get; set; }

    public bool EnvelopeShapeWritten { get; set; }

    public void Reset()
    {
        foreach (var channelState in ChannelStates)
        {
            channelState.Reset();
        }

        NoisePeriod = 0;
        EnvelopePeriod = 0;
        EnvelopeShape = 0;
        EnvelopeShapeWritten = false;
    }
}