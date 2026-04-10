// SPDX-FileCopyrightText: 2026 Anton Shirokov
// SPDX-License-Identifier: MIT

using RetroSound.Core.Models;
using RetroSound.Core.Playback.Pipelines;
using RetroSound.Core.Playback;
using RetroSound.Core.Audio;
using RetroSound.Core.Playback.Sources;
using RetroSound.Core.Rendering;
using RetroSound.NAudio;
using Xunit;
namespace RetroSound.Tests.Playback;

public sealed class DualChipPlaybackPipelineTests
{
    /// <summary>
    /// Verifies that the dual-chip pipeline renders one mixed stereo tick from both emulators.
    /// </summary>
    [Fact]
    public void TryRenderNextTick_RendersOneTickFromBothChipEmulators()
    {
        var player = CreateDualPlayer(sampleRate: 200, chipAFrames: [20], chipBFrames: [60]);
        var chipAEmulator = new TrackingMonoChipEmulator();
        var chipBEmulator = new TrackingStereoChipEmulator();
        var pipeline = new DualChipPlaybackPipeline(player, chipAEmulator, chipBEmulator);
        var buffer = new float[8];

        var rendered = pipeline.TryRenderNextTick(buffer, out var sampleFramesWritten);
        var endOfStream = pipeline.TryRenderNextTick(buffer, out var finalFramesWritten);

        Assert.True(rendered);
        Assert.False(endOfStream);
        Assert.Equal(4, sampleFramesWritten);
        Assert.Equal(0, finalFramesWritten);
        Assert.Equal(1, chipAEmulator.RenderCallCount);
        Assert.Equal(1, chipBEmulator.RenderCallCount);

        Assert.Equal(0.3875f, buffer[0], 6);
        Assert.Equal(0.4625f, buffer[1], 6);
        Assert.Equal(0.3875f, buffer[6], 6);
        Assert.Equal(0.4625f, buffer[7], 6);
    }

    /// <summary>
    /// Verifies that the dual-chip PCM source preserves tick boundaries across partial reads.
    /// </summary>
    [Fact]
    public void Read_PreservesTickBoundariesAcrossPartialReads()
    {
        var player = CreateDualPlayer(sampleRate: 200, chipAFrames: [20, 30], chipBFrames: [60, 80]);
        var source = new DualChipPcmSampleSource(new DualChipPlaybackPipeline(
            player,
            new TrackingMonoChipEmulator(),
            new TrackingStereoChipEmulator()));
        var firstRead = new float[10];
        var secondRead = new float[10];

        var firstCount = source.Read(firstRead);
        var secondCount = source.Read(secondRead);
        var finalCount = source.Read(secondRead);

        Assert.Equal(10, firstCount);
        Assert.Equal(6, secondCount);
        Assert.Equal(0, finalCount);

        Assert.Equal(0.3875f, firstRead[0], 6);
        Assert.Equal(0.4625f, firstRead[1], 6);
        Assert.Equal(0.3875f, firstRead[6], 6);
        Assert.Equal(0.4625f, firstRead[7], 6);
        Assert.Equal(0.5375f, firstRead[8], 6);
        Assert.Equal(0.6125f, firstRead[9], 6);

        Assert.Equal(0.5375f, secondRead[0], 6);
        Assert.Equal(0.6125f, secondRead[1], 6);
        Assert.Equal(0.5375f, secondRead[4], 6);
        Assert.Equal(0.6125f, secondRead[5], 6);
    }

    /// <summary>
    /// Verifies that the NAudio sample provider returns zero cleanly when dual-chip playback has ended.
    /// </summary>
    [Fact]
    public void Read_ReturnsZeroAtEndOfStreamWithoutThrowing()
    {
        var player = CreateDualPlayer(sampleRate: 44_100, chipAFrames: [], chipBFrames: []);
        var provider = new RetroSoundSampleProvider(
            player,
            new TrackingMonoChipEmulator(),
            new TrackingStereoChipEmulator());
        var buffer = new float[32];

        var samplesRead = provider.Read(buffer, 0, buffer.Length);

        Assert.Equal(44_100, provider.WaveFormat.SampleRate);
        Assert.Equal(2, provider.WaveFormat.Channels);
        Assert.Equal(0, samplesRead);
    }

    /// <summary>
    /// Verifies that resetting the dual-chip PCM source restarts buffered playback from the beginning.
    /// </summary>
    [Fact]
    public void Reset_RestartsBufferedPlaybackFromTheBeginning()
    {
        var source = new DualChipPcmSampleSource(new DualChipPlaybackPipeline(
            CreateDualPlayer(sampleRate: 200, chipAFrames: [20, 30], chipBFrames: [60, 80]),
            new TrackingMonoChipEmulator(),
            new TrackingStereoChipEmulator()));
        var firstRead = new float[6];
        var resetRead = new float[6];

        Assert.Equal(6, source.Read(firstRead));

        source.Reset();

        Assert.Equal(6, source.Read(resetRead));
        Assert.Equal(firstRead, resetRead);
    }

    /// <summary>
    /// Verifies that stereo mixing does not clamp values produced by both chip emulators.
    /// </summary>
    [Fact]
    public void TryRenderNextTick_DoesNotClampMixedStereoOutput()
    {
        var pipeline = new DualChipPlaybackPipeline(
            CreateDualPlayer(sampleRate: 200, chipAFrames: [0], chipBFrames: [0]),
            new ConstantStereoChipEmulator(1.5f, 1.5f),
            new ConstantStereoChipEmulator(1.5f, -1.5f));
        var buffer = new float[8];

        Assert.True(pipeline.TryRenderNextTick(buffer, out var sampleFramesWritten));

        Assert.Equal(4, sampleFramesWritten);
        Assert.Equal(1.875f, buffer[0], 6);
        Assert.Equal(-0.375f, buffer[1], 6);
        Assert.Equal(1.875f, buffer[6], 6);
        Assert.Equal(-0.375f, buffer[7], 6);
    }

    /// <summary>
    /// Verifies that stereo samples are mixed independently for the left and right channels.
    /// </summary>
    [Fact]
    public void TryRenderNextTick_MixesStereoOutputFromBothChipsPerChannel()
    {
        var pipeline = new DualChipPlaybackPipeline(
            CreateDualPlayer(sampleRate: 200, chipAFrames: [0], chipBFrames: [0]),
            new ConstantStereoChipEmulator(0.2f, 0.8f),
            new ConstantStereoChipEmulator(0.6f, 0.4f));
        var buffer = new float[8];

        Assert.True(pipeline.TryRenderNextTick(buffer, out var sampleFramesWritten));

        Assert.Equal(4, sampleFramesWritten);
        Assert.Equal(0.35f, buffer[0], 6);
        Assert.Equal(0.65f, buffer[1], 6);
        Assert.Equal(0.35f, buffer[6], 6);
        Assert.Equal(0.65f, buffer[7], 6);
    }

    /// <summary>
    /// Verifies that the frame player rejects payloads that do not contain whole AY register frames.
    /// </summary>
    [Fact]
    public void Constructor_RejectsPartialFrames()
    {
        var exception = Assert.Throws<ArgumentException>(() => new RegisterFramePlayer(new byte[13], 48_000));

        Assert.Equal("frameData", exception.ParamName);
    }

    private static DualChipPlayer CreateDualPlayer(int sampleRate, byte[] chipAFrames, byte[] chipBFrames)
    {
        return new DualChipPlayer(
            new RegisterFramePlayer(CreateFramePayload(chipAFrames), sampleRate),
            new RegisterFramePlayer(CreateFramePayload(chipBFrames), sampleRate));
    }

    private static byte[] CreateFramePayload(byte[] firstRegisterValues)
    {
        var payload = new byte[firstRegisterValues.Length * AyRegisterFrame.RegisterCount];

        for (var frameIndex = 0; frameIndex < firstRegisterValues.Length; frameIndex++)
        {
            payload[frameIndex * AyRegisterFrame.RegisterCount] = firstRegisterValues[frameIndex];
        }

        return payload;
    }

    private sealed class TrackingMonoChipEmulator : IChipEmulator
    {
        public int ChannelCount => 1;

        public int RenderCallCount { get; private set; }

        public void Reset()
        {
        }

        public int Render(AyRegisterFrame frame, PlaybackTiming timing, Span<float> destination)
        {
            var sampleFrames = timing.GetSampleFramesPerTick();
            var sample = frame[0] / 100f;

            for (var index = 0; index < sampleFrames; index++)
            {
                destination[index] = sample;
            }

            RenderCallCount++;
            return sampleFrames;
        }
    }

    private sealed class TrackingStereoChipEmulator : IChipEmulator
    {
        public int ChannelCount => 2;

        public int RenderCallCount { get; private set; }

        public void Reset()
        {
        }

        public int Render(AyRegisterFrame frame, PlaybackTiming timing, Span<float> destination)
        {
            var sampleFrames = timing.GetSampleFramesPerTick();
            var left = frame[0] / 100f;
            var right = (frame[0] + 10) / 100f;

            for (var frameIndex = 0; frameIndex < sampleFrames; frameIndex++)
            {
                var destinationIndex = frameIndex * 2;
                destination[destinationIndex] = left;
                destination[destinationIndex + 1] = right;
            }

            RenderCallCount++;
            return sampleFrames;
        }
    }

    private sealed class ConstantStereoChipEmulator(float left, float right) : IChipEmulator
    {
        public int ChannelCount => 2;

        public void Reset()
        {
        }

        public int Render(AyRegisterFrame frame, PlaybackTiming timing, Span<float> destination)
        {
            var sampleFrames = timing.GetSampleFramesPerTick();

            for (var frameIndex = 0; frameIndex < sampleFrames; frameIndex++)
            {
                var destinationIndex = frameIndex * 2;
                destination[destinationIndex] = left;
                destination[destinationIndex + 1] = right;
            }

            return sampleFrames;
        }
    }
}
