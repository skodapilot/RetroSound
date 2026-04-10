// SPDX-FileCopyrightText: 2026 Anton Shirokov
// SPDX-License-Identifier: MIT

using RetroSound.Core.Models;
using RetroSound.Core.Playback;
using RetroSound.Core.Playback.Tracker;
using RetroSound.Tests.TestDoubles;
using Xunit;
namespace RetroSound.Tests.Playback;

public sealed class DualChipPlayerTests
{
    /// <summary>
    /// Verifies that the dual-chip player advances both child players in lockstep.
    /// </summary>
    [Fact]
    public void TryAdvance_AdvancesBothPlayersInLockstep()
    {
        var chipAPlayer = new TrackingTickPlayer(
            sampleRate: 48_000,
            new byte[] { 0x10, 0x11 },
            new byte[] { 0x20, 0x21 });
        var chipBPlayer = new TrackingTickPlayer(
            sampleRate: 48_000,
            new byte[] { 0x30, 0x31 },
            new byte[] { 0x40, 0x41 });
        var player = new DualChipPlayer(chipAPlayer, chipBPlayer);

        Assert.Equal(50, player.Timing.TicksPerSecond);
        Assert.Equal(48_000, player.Timing.SampleRate);

        Assert.True(player.TryAdvance(out var firstFrame));
        Assert.True(player.TryAdvance(out var secondFrame));
        Assert.False(player.TryAdvance(out _));
        Assert.True(player.IsEndOfStream);

        Assert.Equal(2, chipAPlayer.ProducedTickCount);
        Assert.Equal(2, chipBPlayer.ProducedTickCount);
        Assert.Equal(0x10, firstFrame.ChipA[0]);
        Assert.Equal(0x30, firstFrame.ChipB[0]);
        Assert.Equal(0x20, secondFrame.ChipA[0]);
        Assert.Equal(0x40, secondFrame.ChipB[0]);
    }

    /// <summary>
    /// Verifies that dual-chip playback produces the same frame sequence after a reset.
    /// </summary>
    [Fact]
    public void TryAdvance_ProducesDeterministicDualFramesAcrossResets()
    {
        var player = new DualChipPlayer(
            new SingleChipTrackerPlayer(CreateChipAModule(), sampleRate: 48_000),
            new SingleChipTrackerPlayer(CreateChipBModule(), sampleRate: 48_000));

        var firstPass = ReadAllFrames(player);

        player.Reset();

        var secondPass = ReadAllFrames(player);

        Assert.Equal(3, firstPass.Count);
        Assert.Equal(firstPass.Count, secondPass.Count);

        for (var frameIndex = 0; frameIndex < firstPass.Count; frameIndex++)
        {
            Assert.Equal(firstPass[frameIndex].ChipA.ToArray(), secondPass[frameIndex].ChipA.ToArray());
            Assert.Equal(firstPass[frameIndex].ChipB.ToArray(), secondPass[frameIndex].ChipB.ToArray());
        }
    }

    /// <summary>
    /// Verifies that resetting the dual-chip player restores the initial state of both child players.
    /// </summary>
    [Fact]
    public void Reset_RestoresInitialStateForBothPlayers()
    {
        var chipAPlayer = new SingleChipTrackerPlayer(CreateChipAModule(), sampleRate: 48_000);
        var chipBPlayer = new SingleChipTrackerPlayer(CreateChipBModule(), sampleRate: 48_000);
        var player = new DualChipPlayer(chipAPlayer, chipBPlayer);

        Assert.True(player.TryAdvance(out var firstFrame));
        Assert.True(player.TryAdvance(out _));

        player.Reset();

        Assert.False(player.IsEndOfStream);
        Assert.False(chipAPlayer.IsEndOfStream);
        Assert.False(chipBPlayer.IsEndOfStream);
        Assert.True(player.TryAdvance(out var resetFrame));
        Assert.Equal(firstFrame.ChipA.ToArray(), resetFrame.ChipA.ToArray());
        Assert.Equal(firstFrame.ChipB.ToArray(), resetFrame.ChipB.ToArray());
    }

    /// <summary>
    /// Verifies that both child players must use the same playback timing.
    /// </summary>
    [Fact]
    public void Constructor_RejectsDifferentPlaybackTiming()
    {
        var chipAPlayer = new TestTickPlayer(sampleRate: 48_000, tickCount: 1);
        var chipBPlayer = new TestTickPlayer(sampleRate: 44_100, tickCount: 1);

        var exception = Assert.Throws<ArgumentException>(() => new DualChipPlayer(chipAPlayer, chipBPlayer));

        Assert.Equal("chipBPlayer", exception.ParamName);
    }

    /// <summary>
    /// Verifies that the dual-chip player rejects child players that diverge mid-playback.
    /// </summary>
    [Fact]
    public void TryAdvance_RejectsDivergingPlayers()
    {
        var player = new DualChipPlayer(
            new TestTickPlayer(sampleRate: 48_000, tickCount: 2),
            new TestTickPlayer(sampleRate: 48_000, tickCount: 1));

        Assert.True(player.TryAdvance(out _));

        var exception = Assert.Throws<InvalidOperationException>(() => player.TryAdvance(out _));

        Assert.Equal("The two chip players diverged while advancing dual-chip playback.", exception.Message);
    }

    /// <summary>
    /// Verifies that the dual-chip player propagates loop playback toggles to both child players.
    /// </summary>
    [Fact]
    public void IsLoopingEnabled_UpdatesBothChildPlayers()
    {
        var chipAPlayer = new LoopingTrackingTickPlayer(sampleRate: 48_000, new byte[] { 0x10, 0x11 });
        var chipBPlayer = new LoopingTrackingTickPlayer(sampleRate: 48_000, new byte[] { 0x20, 0x21 });
        var player = new DualChipPlayer(chipAPlayer, chipBPlayer);

        Assert.True(player.SupportsLooping);
        Assert.False(player.IsLoopingEnabled);

        player.IsLoopingEnabled = true;

        Assert.True(chipAPlayer.IsLoopingEnabled);
        Assert.True(chipBPlayer.IsLoopingEnabled);
        Assert.True(player.IsLoopingEnabled);
    }

    private static List<DualChipPlaybackFrame> ReadAllFrames(DualChipPlayer player)
    {
        var frames = new List<DualChipPlaybackFrame>();

        while (player.TryAdvance(out var frame))
        {
            frames.Add(frame);
        }

        return frames;
    }

    private static SingleChipTrackerModule CreateChipAModule()
    {
        return new SingleChipTrackerModule(
            title: "Chip A",
            order: [0, 1],
            patterns:
            [
                new SingleChipPattern(
                [
                    new SingleChipPatternRow(
                        tickDuration: 2,
                        channelA: new SingleChipChannelCommand(
                            tonePeriod: 0x101,
                            volume: 0x08,
                            toneEnabled: true,
                            noiseEnabled: false),
                        channelB: new SingleChipChannelCommand(
                            tonePeriod: 0x202,
                            volume: 0x07,
                            toneEnabled: true,
                            noiseEnabled: true),
                        channelC: new SingleChipChannelCommand(
                            tonePeriod: 0x303,
                            volume: 0x06,
                            toneEnabled: false,
                            noiseEnabled: false),
                        noisePeriod: 0x05,
                        envelopePeriod: 0x1111,
                        envelopeShape: 0x0A),
                ]),
                new SingleChipPattern(
                [
                    new SingleChipPatternRow(
                        tickDuration: 1,
                        channelA: new SingleChipChannelCommand(
                            tonePeriod: 0x404,
                            volume: 0x05,
                            toneEnabled: false,
                            noiseEnabled: false),
                        channelB: new SingleChipChannelCommand(
                            volume: 0x03,
                            toneEnabled: true,
                            noiseEnabled: false),
                        channelC: new SingleChipChannelCommand(
                            tonePeriod: 0x505,
                            volume: 0x02,
                            toneEnabled: true,
                            noiseEnabled: true),
                        noisePeriod: 0x0E),
                ]),
            ]);
    }

    private static SingleChipTrackerModule CreateChipBModule()
    {
        return new SingleChipTrackerModule(
            title: "Chip B",
            order: [0, 1],
            patterns:
            [
                new SingleChipPattern(
                [
                    new SingleChipPatternRow(
                        tickDuration: 1,
                        channelA: new SingleChipChannelCommand(
                            tonePeriod: 0x123,
                            volume: 0x0F,
                            toneEnabled: true,
                            noiseEnabled: true),
                        channelB: new SingleChipChannelCommand(
                            tonePeriod: 0x234,
                            volume: 0x04,
                            toneEnabled: false,
                            noiseEnabled: false),
                        channelC: new SingleChipChannelCommand(
                            tonePeriod: 0x345,
                            volume: 0x01,
                            toneEnabled: true,
                            noiseEnabled: false),
                        noisePeriod: 0x07,
                        envelopePeriod: 0x2222,
                        envelopeShape: 0x05),
                ]),
                new SingleChipPattern(
                [
                    new SingleChipPatternRow(
                        tickDuration: 2,
                        channelA: new SingleChipChannelCommand(
                            tonePeriod: 0x456,
                            volume: 0x0C,
                            toneEnabled: true,
                            noiseEnabled: false),
                        channelB: new SingleChipChannelCommand(
                            tonePeriod: 0x567,
                            volume: 0x0B,
                            toneEnabled: true,
                            noiseEnabled: true),
                        channelC: new SingleChipChannelCommand(
                            volume: 0x00,
                            toneEnabled: false,
                            noiseEnabled: false),
                        noisePeriod: 0x08),
                ]),
            ]);
    }

    private class TrackingTickPlayer : ITickPlayer
    {
        private readonly byte[][] _ticks;
        private int _nextTickIndex;

        public TrackingTickPlayer(int sampleRate, params byte[][] ticks)
        {
            Timing = new PlaybackTiming(50, sampleRate);
            _ticks = ticks;
        }

        public PlaybackTiming Timing { get; }

        public bool IsEndOfStream => _nextTickIndex >= _ticks.Length;

        public int ProducedTickCount => _nextTickIndex;

        public void Reset()
        {
            _nextTickIndex = 0;
        }

        public bool TryAdvance(out AyRegisterFrame frame)
        {
            if (IsEndOfStream)
            {
                frame = null!;
                return false;
            }

            Span<byte> registers = stackalloc byte[AyRegisterFrame.RegisterCount];
            registers.Fill(_ticks[_nextTickIndex][0]);
            registers[0] = _ticks[_nextTickIndex][0];
            registers[1] = _ticks[_nextTickIndex][1];

            frame = new AyRegisterFrame(registers);
            _nextTickIndex++;
            return true;
        }
    }

    private sealed class LoopingTrackingTickPlayer : TrackingTickPlayer, ILoopingPlaybackController
    {
        public LoopingTrackingTickPlayer(int sampleRate, params byte[][] ticks)
            : base(sampleRate, ticks)
        {
        }

        public bool SupportsLooping => true;

        public bool IsLoopingEnabled { get; set; }
    }
}
