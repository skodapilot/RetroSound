// SPDX-FileCopyrightText: 2026 Anton Shirokov
// SPDX-License-Identifier: MIT

using RetroSound.Core.Formats.Pt3;
using RetroSound.Core.Playback;
using Xunit;
namespace RetroSound.Tests.Playback;

public sealed class Pt3PlayerSamplePlaybackTests
{
    /// <summary>
    /// Verifies that sample-step mixer bits affect tone, noise, and envelope output as expected.
    /// </summary>
    [Fact]
    public void TryAdvance_UsesSampleStepMixerMasksForToneNoiseAndEnvelope()
    {
        var module = new Pt3Module(
            new Pt3ModuleMetadata("7", "PT3 test", "RetroSound"),
            Pt3FrequencyTableKind.ProTracker,
            tempo: 1,
            restartPositionIndex: 0,
            order: [0],
            patterns:
            [
                new Pt3Pattern(
                    0,
                    new Pt3ChannelPattern("A", 0, new byte[] { 0x11, 0x00, 0x20, 0x02, 0x50, 0x00 }),
                    new Pt3ChannelPattern("B", 0, new byte[] { 0x00 }),
                    new Pt3ChannelPattern("C", 0, new byte[] { 0x00 }))
            ],
            samples:
            [
                new Pt3Sample(1, 0, 0, [new Pt3SampleStep(flags: 0x01, mixerAndVolume: 0x90, toneOffset: 0)])
            ],
            ornaments:
            [
                new Pt3Ornament(0, 0, 0, [(sbyte)0])
            ]);

        var player = new Pt3Player(module, sampleRate: 48_000);

        Assert.True(player.TryAdvance(out var frame));

        Assert.Equal(0x09, frame[7] & 0x09);
        Assert.Equal(0x00, frame[8] & 0x10);
        Assert.Equal(0x00, frame[8] & 0x0F);
    }

    /// <summary>
    /// Verifies that sample tone offsets accumulate when the sample flags request accumulation.
    /// </summary>
    [Fact]
    public void TryAdvance_AccumulatesSampleToneOffsetWhenRequested()
    {
        var module = new Pt3Module(
            new Pt3ModuleMetadata("7", "PT3 test", "RetroSound"),
            Pt3FrequencyTableKind.ProTracker,
            tempo: 1,
            restartPositionIndex: 0,
            order: [0],
            patterns:
            [
                new Pt3Pattern(
                    0,
                    new Pt3ChannelPattern("A", 0, new byte[] { 0xD1, 0x50, 0xB1, 0x01, 0x00 }),
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
                        new Pt3SampleStep(flags: 0x00, mixerAndVolume: 0x40 | 0x0F, toneOffset: 2),
                        new Pt3SampleStep(flags: 0x00, mixerAndVolume: 0x40 | 0x0F, toneOffset: 2)
                    ])
            ],
            ornaments:
            [
                new Pt3Ornament(0, 0, 0, [(sbyte)0])
            ]);

        var player = new Pt3Player(module, sampleRate: 48_000);

        Assert.True(player.TryAdvance(out var firstFrame));
        Assert.True(player.TryAdvance(out var secondFrame));

        var firstTone = firstFrame[0] | ((firstFrame[1] & 0x0F) << 8);
        var secondTone = secondFrame[0] | ((secondFrame[1] & 0x0F) << 8);

        Assert.Equal(2, secondTone - firstTone);
    }
}
