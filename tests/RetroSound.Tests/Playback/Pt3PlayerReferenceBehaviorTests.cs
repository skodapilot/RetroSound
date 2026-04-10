// SPDX-FileCopyrightText: 2026 Anton Shirokov
// SPDX-License-Identifier: MIT

using System.Reflection;
using RetroSound.Core.Formats.Pt3;
using RetroSound.Core.Models;
using RetroSound.Core.Playback;
using Xunit;
namespace RetroSound.Tests.Playback;

public sealed class Pt3PlayerReferenceBehaviorTests
{
    /// <summary>
    /// Verifies that the B1 pause command keeps suppressing note changes across following rows.
    /// </summary>
    [Fact]
    public void TryAdvance_B1KeepsApplyingPauseAcrossFollowingRows()
    {
        var player = new Pt3Player(
            CreateModule(
                new byte[] { 0xB1, 0x03, 0x50, 0x51, 0x00 },
                sample1Steps:
                [
                    new Pt3SampleStep(flags: 0x00, mixerAndVolume: 0x0F, toneOffset: 0)
                ]),
            sampleRate: 48_000);

        Assert.True(player.TryAdvance(out var firstFrame));
        Assert.True(player.TryAdvance(out var secondFrame));
        Assert.True(player.TryAdvance(out var thirdFrame));
        Assert.True(player.TryAdvance(out var fourthFrame));

        Assert.Equal(GetTonePeriod(firstFrame), GetTonePeriod(secondFrame));
        Assert.Equal(GetTonePeriod(firstFrame), GetTonePeriod(thirdFrame));
        Assert.NotEqual(GetTonePeriod(firstFrame), GetTonePeriod(fourthFrame));
    }

    /// <summary>
    /// Verifies that command 10 clears the active ornament and returns to the default ornament.
    /// </summary>
    [Fact]
    public void TryAdvance_Command10ResetsOrnamentToClear()
    {
        var player = new Pt3Player(
            CreateModule(
                new byte[] { 0x41, 0x50, 0x10, 0x02, 0x50, 0x00 },
                sample1Steps:
                [
                    new Pt3SampleStep(flags: 0x00, mixerAndVolume: 0x0F, toneOffset: 0)
                ],
                ornaments:
                [
                    new Pt3Ornament(0, 0, 0, [(sbyte)0]),
                    new Pt3Ornament(1, 0, 0, [(sbyte)12])
                ]),
            sampleRate: 48_000);

        Assert.True(player.TryAdvance(out var ornamentFrame));
        Assert.True(player.TryAdvance(out var clearFrame));

        Assert.NotEqual(GetTonePeriod(ornamentFrame), GetTonePeriod(clearFrame));
        Assert.Equal(0, GetChannelStateIntProperty(player, "OrnamentIndex"));
    }

    /// <summary>
    /// Verifies that switching samples preserves the current position inside the sample sequence.
    /// </summary>
    [Fact]
    public void TryAdvance_SampleChangeKeepsCurrentSamplePosition()
    {
        var player = new Pt3Player(
            CreateModule(
                new byte[] { 0x50, 0xD2, 0x00 },
                sample1Steps:
                [
                    new Pt3SampleStep(flags: 0x00, mixerAndVolume: 0x01, toneOffset: 0),
                    new Pt3SampleStep(flags: 0x00, mixerAndVolume: 0x04, toneOffset: 0)
                ],
                sample2Steps:
                [
                    new Pt3SampleStep(flags: 0x00, mixerAndVolume: 0x02, toneOffset: 0),
                    new Pt3SampleStep(flags: 0x00, mixerAndVolume: 0x07, toneOffset: 0),
                    new Pt3SampleStep(flags: 0x00, mixerAndVolume: 0x08, toneOffset: 0)
                ]),
            sampleRate: 48_000);

        Assert.True(player.TryAdvance(out _));
        Assert.True(player.TryAdvance(out _));

        Assert.Equal(2, GetChannelStateIntProperty(player, "SampleIndex"));
        Assert.Equal(2, GetChannelStateIntProperty(player, "SampleStepIndex"));
    }

    private static Pt3Module CreateModule(
        byte[] channelAStream,
        Pt3SampleStep[] sample1Steps,
        Pt3SampleStep[]? sample2Steps = null,
        Pt3Ornament[]? ornaments = null)
    {
        var samples = new List<Pt3Sample>
        {
            new(1, 0, 0, sample1Steps)
        };

        if (sample2Steps is not null)
        {
            samples.Add(new Pt3Sample(2, 0, 0, sample2Steps));
        }

        return new Pt3Module(
            new Pt3ModuleMetadata("7", "PT3 reference behavior", "RetroSound"),
            Pt3FrequencyTableKind.ProTracker,
            tempo: 1,
            restartPositionIndex: 0,
            order: [0],
            patterns:
            [
                new Pt3Pattern(
                    0,
                    new Pt3ChannelPattern("A", 0, channelAStream),
                    new Pt3ChannelPattern("B", 0, new byte[] { 0x00 }),
                    new Pt3ChannelPattern("C", 0, new byte[] { 0x00 }))
            ],
            samples,
            ornaments ?? [new Pt3Ornament(0, 0, 0, [(sbyte)0])]);
    }

    private static int GetChannelStateIntProperty(Pt3Player player, string propertyName)
    {
        var statesField = typeof(Pt3Player).GetField("_channelStates", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(statesField);

        var states = Assert.IsAssignableFrom<Array>(statesField.GetValue(player));
        var channelState = states.GetValue(0);
        Assert.NotNull(channelState);

        var property = channelState.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(property);

        return Assert.IsType<int>(property.GetValue(channelState));
    }

    private static int GetTonePeriod(AyRegisterFrame frame)
    {
        return frame[0] | ((frame[1] & 0x0F) << 8);
    }
}
