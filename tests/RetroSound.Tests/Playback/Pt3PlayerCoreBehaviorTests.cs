// SPDX-FileCopyrightText: 2026 Anton Shirokov
// SPDX-License-Identifier: MIT

using RetroSound.Core.Formats.Pt3;
using RetroSound.Core.Playback;
using Xunit;
namespace RetroSound.Tests.Playback;

public sealed class Pt3PlayerCoreBehaviorTests
{
    /// <summary>
    /// Verifies that an envelope sample command does not consume an extra delay byte.
    /// </summary>
    [Fact]
    public void TryAdvance_ParsesEnvelopeSampleCommandWithoutConsumingAnExtraDelayByte()
    {
        var loader = new Pt3ModuleLoader();
        var module = loader.Load(CreateEnvelopeSampleModule());
        var player = new Pt3Player(module, sampleRate: 48_000);

        Assert.True(player.TryAdvance(out var frame));
        Assert.True(player.TryAdvance(out var loopedFrame));
        Assert.False(player.IsEndOfStream);

        Assert.NotEqual(0, frame[8] & 0x0F);
        Assert.Equal(0x10, frame[11]);
        Assert.Equal(0x00, frame[12]);
        Assert.Equal(0x0A, frame[13]);
        Assert.True(loopedFrame.EnvelopeShapeWritten);
    }

    /// <summary>
    /// Verifies that playback can stop at the end of the order list instead of looping.
    /// </summary>
    [Fact]
    public void TryAdvance_StopsAfterLastOrderInsteadOfLooping()
    {
        var module = new Pt3Module(
            new Pt3ModuleMetadata("7", "End of stream", "RetroSound"),
            Pt3FrequencyTableKind.ProTracker,
            tempo: 1,
            restartPositionIndex: 0,
            order: [0],
            patterns:
            [
                new Pt3Pattern(
                    0,
                    new Pt3ChannelPattern("A", 0, new byte[] { 0xD1, 0x50, 0x00 }),
                    new Pt3ChannelPattern("B", 0, new byte[] { 0x00 }),
                    new Pt3ChannelPattern("C", 0, new byte[] { 0x00 }))
            ],
            samples:
            [
                new Pt3Sample(1, 0, 0, [new Pt3SampleStep(flags: 0x00, mixerAndVolume: 0x0F, toneOffset: 0)])
            ],
            ornaments:
            [
                new Pt3Ornament(0, 0, 0, [(sbyte)0])
            ]);
        var player = new Pt3Player(module, sampleRate: 48_000, stopAfterOrderList: true);

        Assert.True(player.TryAdvance(out _));
        Assert.False(player.IsEndOfStream);

        Assert.False(player.TryAdvance(out _));
        Assert.True(player.IsEndOfStream);
    }

    /// <summary>
    /// Verifies that loop playback can be enabled at runtime even when the player starts in stop-at-end mode.
    /// </summary>
    [Fact]
    public void TryAdvance_LoopsWhenLoopPlaybackIsEnabledAtRuntime()
    {
        var module = CreateSingleRowModule();
        var player = new Pt3Player(module, sampleRate: 48_000, stopAfterOrderList: true)
        {
            IsLoopingEnabled = true,
        };

        Assert.True(player.TryAdvance(out var firstFrame));
        Assert.True(player.TryAdvance(out var secondFrame));
        Assert.False(player.IsEndOfStream);

        Assert.Equal(firstFrame.ToArray(), secondFrame.ToArray());
    }

    /// <summary>
    /// Verifies that the constructor keeps the historical stop-after-order behavior disabled by default.
    /// </summary>
    [Fact]
    public void Constructor_EnablesLoopPlaybackByDefault()
    {
        var player = new Pt3Player(CreateSingleRowModule(), sampleRate: 48_000);

        Assert.True(player.IsLoopingEnabled);
        Assert.True(player.SupportsLooping);
    }

    private static byte[] CreateEnvelopeSampleModule()
    {
        var data = File.ReadAllBytes(GetTestDataPath("minimal-valid.pt3"));
        var expanded = new byte[232];

        Array.Copy(data, expanded, 209);
        expanded[100] = 0x01;

        expanded[107] = 216;
        expanded[108] = 0;
        expanded[109] = 222;
        expanded[110] = 0;
        expanded[169] = 229;
        expanded[170] = 0;

        expanded[203] = 209;
        expanded[204] = 0;
        expanded[205] = 214;
        expanded[206] = 0;
        expanded[207] = 215;
        expanded[208] = 0;

        expanded[209] = 0x1A;
        expanded[210] = 0x00;
        expanded[211] = 0x10;
        expanded[212] = 0x02;
        expanded[213] = 0x50;
        expanded[214] = 0x00;
        expanded[215] = 0x00;

        expanded[216] = 0x00;
        expanded[217] = 0x01;
        expanded[218] = 0x00;
        expanded[219] = 0x0F;
        expanded[220] = 0x00;
        expanded[221] = 0x00;

        expanded[222] = 0x00;
        expanded[223] = 0x01;
        expanded[224] = 0x00;
        expanded[225] = 0x0F;
        expanded[226] = 0x00;
        expanded[227] = 0x00;
        expanded[228] = 0x00;
        expanded[229] = 0x00;
        expanded[230] = 0x01;
        expanded[231] = 0x00;

        return expanded;
    }

    private static string GetTestDataPath(string fileName)
    {
        return Path.Combine(AppContext.BaseDirectory, "TestData", fileName);
    }

    private static Pt3Module CreateSingleRowModule()
    {
        return new Pt3Module(
            new Pt3ModuleMetadata("7", "Loop test", "RetroSound"),
            Pt3FrequencyTableKind.ProTracker,
            tempo: 1,
            restartPositionIndex: 0,
            order: [0],
            patterns:
            [
                new Pt3Pattern(
                    0,
                    new Pt3ChannelPattern("A", 0, new byte[] { 0xD1, 0x50, 0x00 }),
                    new Pt3ChannelPattern("B", 0, new byte[] { 0x00 }),
                    new Pt3ChannelPattern("C", 0, new byte[] { 0x00 }))
            ],
            samples:
            [
                new Pt3Sample(1, 0, 0, [new Pt3SampleStep(flags: 0x00, mixerAndVolume: 0x0F, toneOffset: 0)])
            ],
            ornaments:
            [
                new Pt3Ornament(0, 0, 0, [(sbyte)0])
            ]);
    }
}
