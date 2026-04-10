// SPDX-FileCopyrightText: 2026 Anton Shirokov
// SPDX-License-Identifier: MIT

using RetroSound.Core.Formats.Pt3;
using RetroSound.Core.Playback;
using Xunit;
namespace RetroSound.Tests.Playback;

public sealed class Pt3PlayerCommandParsingTests
{
    /// <summary>
    /// Verifies that row effect parameters are consumed in the same reverse order as the tracker format.
    /// </summary>
    [Fact]
    public void TryAdvance_ReadsMultipleEffectParametersInReverseTrackerOrder()
    {
        var module = new Pt3Module(
            new Pt3ModuleMetadata("7", "PT3 test", "RetroSound"),
            Pt3FrequencyTableKind.ProTracker,
            tempo: 6,
            restartPositionIndex: 0,
            order: [0],
            patterns:
            [
                new Pt3Pattern(
                    0,
                    new Pt3ChannelPattern("A", 0, new byte[] { 0x03, 0x09, 0x50, 0x07, 0x03, 0x00 }),
                    new Pt3ChannelPattern("B", 0, new byte[] { 0x00 }),
                    new Pt3ChannelPattern("C", 0, new byte[] { 0x00 }))
            ],
            samples:
            [
                new Pt3Sample(
                    1,
                    0,
                    0,
                    [
                        new Pt3SampleStep(flags: 0x00, mixerAndVolume: 0x01, toneOffset: 100),
                        new Pt3SampleStep(flags: 0x00, mixerAndVolume: 0x02, toneOffset: 0),
                        new Pt3SampleStep(flags: 0x00, mixerAndVolume: 0x03, toneOffset: 0),
                        new Pt3SampleStep(flags: 0x00, mixerAndVolume: 0x04, toneOffset: 0),
                        new Pt3SampleStep(flags: 0x00, mixerAndVolume: 0x05, toneOffset: 0),
                        new Pt3SampleStep(flags: 0x00, mixerAndVolume: 0x06, toneOffset: 0),
                        new Pt3SampleStep(flags: 0x00, mixerAndVolume: 0x07, toneOffset: 0),
                        new Pt3SampleStep(flags: 0x00, mixerAndVolume: 0x08, toneOffset: 0)
                    ])
            ],
            ornaments:
            [
                new Pt3Ornament(0, 0, 0, [(sbyte)0])
            ]);

        var player = new Pt3Player(module, sampleRate: 48_000);

        Assert.True(player.TryAdvance(out var frame));
        Assert.Equal(4, frame[8] & 0x0F);
    }
}
