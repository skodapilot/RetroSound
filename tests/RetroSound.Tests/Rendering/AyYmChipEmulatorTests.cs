// SPDX-FileCopyrightText: 2026 Anton Shirokov
// SPDX-License-Identifier: MIT

using RetroSound.Core.Models;
using RetroSound.Core.Rendering;
using RetroSound.Tests.TestDoubles;
using Xunit;
namespace RetroSound.Tests.Rendering;

public sealed class AyYmChipEmulatorTests
{
    /// <summary>
    /// Verifies that the emulator initializes and uses a stereo backend with the configured channel count.
    /// </summary>
    [Fact]
    public void Render_UsesTheConfiguredStereoBackend()
    {
        using var emulator = new AyYmChipEmulator(
            new TestAyYmSampleRendererBackend(),
            new AyYmChipConfiguration(outputChannelCount: 2));

        var timing = new PlaybackTiming(50, 48_000);
        var frame = new AyRegisterFrame(new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13 });
        var buffer = new float[1_920];

        var sampleFramesWritten = emulator.Render(frame, timing, buffer);

        Assert.Equal(2, emulator.ChannelCount);
        Assert.Equal(960, sampleFramesWritten);
        Assert.Equal(910f / 2048f, buffer[0], 6);
        Assert.Equal((910f / 2048f) + (1f / 4096f), buffer[1], 6);
        Assert.Equal(911f / 2048f, buffer[2], 6);
        Assert.Equal((911f / 2048f) + (1f / 4096f), buffer[3], 6);
    }

    /// <summary>
    /// Verifies that the emulator rejects backends that do not render a full playback tick.
    /// </summary>
    [Fact]
    public void Render_RejectsBackendsThatDoNotRenderAWholeTick()
    {
        using var emulator = new AyYmChipEmulator(
            new ShortWriteBackend(),
            new AyYmChipConfiguration(outputChannelCount: 1));

        var timing = new PlaybackTiming(50, 48_000);
        var frame = new AyRegisterFrame(new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13 });

        var exception = Assert.Throws<InvalidOperationException>(() => emulator.Render(frame, timing, new float[960]));

        Assert.Equal(
            "The AY/YM renderer backend must render exactly one tick of audio for each register frame.",
            exception.Message);
    }

    /// <summary>
    /// Verifies that the backend channel count must match the configured chip output channel count.
    /// </summary>
    [Fact]
    public void Constructor_RejectsBackendChannelCountMismatch()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => new AyYmChipEmulator(
            new MismatchedChannelCountBackend(),
            new AyYmChipConfiguration(outputChannelCount: 2)));

        Assert.Equal(
            "The AY/YM renderer backend channel count must match the configured output channel count.",
            exception.Message);
    }

    /// <summary>
    /// Verifies that the emulator rejects API calls after it has been disposed.
    /// </summary>
    [Fact]
    public void Reset_RejectsCallsAfterDispose()
    {
        var emulator = new AyYmChipEmulator(
            new TestAyYmSampleRendererBackend(),
            new AyYmChipConfiguration(outputChannelCount: 1));
        emulator.Dispose();

        Assert.Throws<ObjectDisposedException>(() => emulator.Reset());
    }

    private sealed class ShortWriteBackend : IAyYmSampleRendererBackend
    {
        public int ChannelCount { get; private set; }

        public void Initialize(AyYmChipConfiguration configuration)
        {
            ChannelCount = configuration.OutputChannelCount;
        }

        public void Reset()
        {
        }

        public int Render(AyRegisterFrame frame, PlaybackTiming timing, Span<float> destination)
        {
            return 1;
        }

        public void Dispose()
        {
        }
    }

    private sealed class MismatchedChannelCountBackend : IAyYmSampleRendererBackend
    {
        public int ChannelCount => 1;

        public void Initialize(AyYmChipConfiguration configuration)
        {
        }

        public void Reset()
        {
        }

        public int Render(AyRegisterFrame frame, PlaybackTiming timing, Span<float> destination)
        {
            return 0;
        }

        public void Dispose()
        {
        }
    }
}
