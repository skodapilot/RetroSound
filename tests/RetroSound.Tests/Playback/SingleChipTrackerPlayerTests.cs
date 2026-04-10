// SPDX-FileCopyrightText: 2026 Anton Shirokov
// SPDX-License-Identifier: MIT

using RetroSound.Core.Models;
using RetroSound.Core.Playback.Tracker;
using Xunit;
namespace RetroSound.Tests.Playback;

public sealed class SingleChipTrackerPlayerTests
{
    /// <summary>
    /// Verifies that resetting the tracker player restores the initial playback state.
    /// </summary>
    [Fact]
    public void Reset_RestoresInitialPlaybackState()
    {
        var player = new SingleChipTrackerPlayer(CreateModule(), sampleRate: 48_000);

        Assert.True(player.TryAdvance(out var firstFrame));
        Assert.True(player.TryAdvance(out _));

        player.Reset();

        Assert.False(player.IsEndOfStream);
        Assert.True(player.TryAdvance(out var resetFrame));
        Assert.Equal(firstFrame.ToArray(), resetFrame.ToArray());
    }

    /// <summary>
    /// Verifies that the tracker player produces the same AY register sequence after a reset.
    /// </summary>
    [Fact]
    public void TryAdvance_ProducesDeterministicSequenceAcrossResets()
    {
        var player = new SingleChipTrackerPlayer(CreateModule(), sampleRate: 48_000);

        var firstPass = ReadAllFrames(player);

        player.Reset();

        var secondPass = ReadAllFrames(player);

        Assert.Equal(3, firstPass.Count);
        Assert.Equal(firstPass.Count, secondPass.Count);

        for (var frameIndex = 0; frameIndex < firstPass.Count; frameIndex++)
        {
            Assert.Equal(firstPass[frameIndex], secondPass[frameIndex]);
        }

        Assert.True(player.IsEndOfStream);
        Assert.False(player.TryAdvance(out _));
    }

    /// <summary>
    /// Verifies that the tracker player emits the expected AY register frames for the test module.
    /// </summary>
    [Fact]
    public void TryAdvance_EmitsStableExpectedAyRegisterFrames()
    {
        var player = new SingleChipTrackerPlayer(CreateModule(), sampleRate: 48_000);

        Assert.Equal(50, player.Timing.TicksPerSecond);
        Assert.Equal(48_000, player.Timing.SampleRate);

        Assert.True(player.TryAdvance(out var firstFrame));
        Assert.True(player.TryAdvance(out var secondFrame));
        Assert.True(player.TryAdvance(out var thirdFrame));
        Assert.True(player.IsEndOfStream);
        Assert.False(player.TryAdvance(out _));

        AssertFrame(firstFrame, 0x123, 0x234, 0x345, 0x1C, 0x2C, 0x0A, 0x06, 0x01, 0x4567, 0x09);
        AssertFrame(secondFrame, 0x125, 0x234, 0x345, 0x1C, 0x2C, 0x0A, 0x06, 0x01, 0x4567, 0x09);
        AssertFrame(thirdFrame, 0x127, 0x234, 0x222, 0x03, 0x3A, 0x1C, 0x00, 0x0F, 0x4567, 0x09);
    }

    /// <summary>
    /// Verifies that the tracker module exposes read-only collections for its public state.
    /// </summary>
    [Fact]
    public void ModuleAndPattern_PublicCollectionsExposeReadOnlyViews()
    {
        var module = CreateModule();

        Assert.IsNotType<int[]>(module.Order);
        Assert.IsNotType<SingleChipPattern[]>(module.Patterns);
        Assert.IsNotType<SingleChipPatternRow[]>(module.Patterns[0].Rows);
    }

    private static SingleChipTrackerModule CreateModule()
    {
        return new SingleChipTrackerModule(
            title: "Test module",
            order: [0, 1],
            patterns:
            [
                new SingleChipPattern(
                [
                    new SingleChipPatternRow(
                        tickDuration: 2,
                        channelA: new SingleChipChannelCommand(
                            tonePeriod: 0x123,
                            volume: 0x0A,
                            toneEnabled: true,
                            noiseEnabled: false,
                            effect: new SingleChipChannelEffect(SingleChipChannelEffectType.ToneSlide, 2)),
                        channelB: new SingleChipChannelCommand(
                            tonePeriod: 0x234,
                            volume: 0x06,
                            toneEnabled: true,
                            noiseEnabled: true),
                        channelC: new SingleChipChannelCommand(
                            tonePeriod: 0x345,
                            volume: 0x01,
                            toneEnabled: false,
                            noiseEnabled: false),
                        noisePeriod: 0x1C,
                        envelopePeriod: 0x4567,
                        envelopeShape: 0x09),
                ]),
                new SingleChipPattern(
                [
                    new SingleChipPatternRow(
                        tickDuration: 1,
                        channelA: new SingleChipChannelCommand(
                            volume: 0x0C,
                            useEnvelope: true,
                            effect: new SingleChipChannelEffect(SingleChipChannelEffectType.VolumeSlide, -2)),
                        channelB: new SingleChipChannelCommand(
                            volume: 0x00,
                            toneEnabled: false,
                            noiseEnabled: false),
                        channelC: new SingleChipChannelCommand(
                            tonePeriod: 0x222,
                            volume: 0x0F,
                            toneEnabled: true,
                            noiseEnabled: false),
                        noisePeriod: 0x03),
                ]),
            ]);
    }

    private static List<byte[]> ReadAllFrames(SingleChipTrackerPlayer player)
    {
        var frames = new List<byte[]>();

        while (player.TryAdvance(out var frame))
        {
            frames.Add(frame.ToArray());
        }

        return frames;
    }

    private static void AssertFrame(
        AyRegisterFrame frame,
        int toneA,
        int toneB,
        int toneC,
        int noisePeriod,
        int mixer,
        int volumeA,
        int volumeB,
        int volumeC,
        int envelopePeriod,
        int envelopeShape)
    {
        Assert.Equal(toneA & 0xFF, frame[0]);
        Assert.Equal((toneA >> 8) & 0x0F, frame[1]);
        Assert.Equal(toneB & 0xFF, frame[2]);
        Assert.Equal((toneB >> 8) & 0x0F, frame[3]);
        Assert.Equal(toneC & 0xFF, frame[4]);
        Assert.Equal((toneC >> 8) & 0x0F, frame[5]);
        Assert.Equal(noisePeriod & 0x1F, frame[6]);
        Assert.Equal(mixer, frame[7]);
        Assert.Equal(volumeA, frame[8]);
        Assert.Equal(volumeB, frame[9]);
        Assert.Equal(volumeC, frame[10]);
        Assert.Equal(envelopePeriod & 0xFF, frame[11]);
        Assert.Equal((envelopePeriod >> 8) & 0xFF, frame[12]);
        Assert.Equal(envelopeShape & 0xFF, frame[13]);
    }
}