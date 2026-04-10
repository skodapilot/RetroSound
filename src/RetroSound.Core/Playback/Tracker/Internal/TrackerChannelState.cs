// SPDX-FileCopyrightText: 2026 Anton Shirokov
// SPDX-License-Identifier: MIT

namespace RetroSound.Core.Playback.Tracker.Internal;

internal sealed class TrackerChannelState
{
    public int TonePeriod { get; set; }

    public int Volume { get; set; }

    public bool ToneEnabled { get; set; }

    public bool NoiseEnabled { get; set; }

    public bool UseEnvelope { get; set; }

    public SingleChipChannelEffect Effect { get; set; }

    public void Reset()
    {
        TonePeriod = 0;
        Volume = 0;
        ToneEnabled = false;
        NoiseEnabled = false;
        UseEnvelope = false;
        Effect = SingleChipChannelEffect.None;
    }
}