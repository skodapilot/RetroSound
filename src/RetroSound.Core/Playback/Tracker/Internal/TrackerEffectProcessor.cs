// SPDX-FileCopyrightText: 2026 Anton Shirokov
// SPDX-License-Identifier: MIT

namespace RetroSound.Core.Playback.Tracker.Internal;

internal static class TrackerEffectProcessor
{
    public static void Apply(TrackerChannelState channel)
    {
        switch (channel.Effect.Type)
        {
            case SingleChipChannelEffectType.None:
                return;
            case SingleChipChannelEffectType.ToneSlide:
                channel.TonePeriod = Math.Clamp(channel.TonePeriod + channel.Effect.Delta, 0, 0x0FFF);
                return;
            case SingleChipChannelEffectType.VolumeSlide:
                channel.Volume = Math.Clamp(channel.Volume + channel.Effect.Delta, 0, 0x0F);
                return;
            default:
                throw new InvalidOperationException($"Unsupported effect type '{channel.Effect.Type}'.");
        }
    }
}