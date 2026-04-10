// SPDX-FileCopyrightText: 2026 Anton Shirokov
// SPDX-License-Identifier: MIT

using RetroSound.Core.Playback.Pipelines;
using RetroSound.Core.Audio;
using RetroSound.Core.Playback.Sources;
using RetroSound.Core.Rendering;
using RetroSound.NAudio;
using RetroSound.Tests.TestDoubles;
using Xunit;
namespace RetroSound.Tests.Playback;

public sealed class TickPlaybackPipelineTests
{
    /// <summary>
    /// Verifies that the single-chip pipeline renders all available ticks and reports end of stream.
    /// </summary>
    [Fact]
    public void TryRenderNextTick_RendersEachAvailableTickAndStopsAtEndOfStream()
    {
        var player = new TestTickPlayer(sampleRate: 48_000, tickCount: 2);
        var emulator = new TestChipEmulator();
        var pipeline = new TickPlaybackPipeline(player, emulator);

        var firstTickBuffer = new float[960];
        var secondTickBuffer = new float[960];

        var firstRendered = pipeline.TryRenderNextTick(firstTickBuffer, out var firstSampleFramesWritten);
        var secondRendered = pipeline.TryRenderNextTick(secondTickBuffer, out var secondSampleFramesWritten);
        var noMoreTicks = pipeline.TryRenderNextTick(secondTickBuffer, out var finalSampleFramesWritten);

        Assert.True(firstRendered);
        Assert.True(secondRendered);
        Assert.False(noMoreTicks);

        Assert.Equal(960, firstSampleFramesWritten);
        Assert.Equal(960, secondSampleFramesWritten);
        Assert.Equal(0, finalSampleFramesWritten);

        Assert.Equal(910f / 2048f, firstTickBuffer[0], 6);
        Assert.Equal(2590f % 2048 / 2048f, secondTickBuffer[0], 6);
        Assert.Equal((2590f + 959f) % 2048f / 2048f, secondTickBuffer[959], 6);
    }

    /// <summary>
    /// Verifies that the PCM source keeps unread samples buffered between read calls.
    /// </summary>
    [Fact]
    public void Read_BuffersRemainingSamplesAcrossCalls()
    {
        var player = new TestTickPlayer(sampleRate: 48_000, tickCount: 2);
        var emulator = new TestChipEmulator();
        var pipeline = new TickPlaybackPipeline(player, emulator);
        var source = new PipelinePcmSampleSource(pipeline);
        var firstRead = new float[100];
        var secondRead = new float[1_900];

        var firstCount = source.Read(firstRead);
        var secondCount = source.Read(secondRead);
        var finalCount = source.Read(secondRead);

        Assert.Equal(100, firstCount);
        Assert.Equal(1_820, secondCount);
        Assert.Equal(0, finalCount);

        Assert.Equal(910f / 2048f, firstRead[0], 6);
        Assert.Equal((910f + 99f) / 2048f, firstRead[99], 6);
        Assert.Equal((910f + 100f) / 2048f, secondRead[0], 6);
        Assert.Equal(2590f % 2048 / 2048f, secondRead[860], 6);
    }

    /// <summary>
    /// Verifies that resetting the PCM source drops buffered samples and restarts playback.
    /// </summary>
    [Fact]
    public void Reset_DiscardsBufferedTickData()
    {
        var player = new TestTickPlayer(sampleRate: 48_000, tickCount: 2);
        var emulator = new TestChipEmulator();
        var pipeline = new TickPlaybackPipeline(player, emulator);
        var source = new PipelinePcmSampleSource(pipeline);
        var partialRead = new float[100];
        var resetRead = new float[100];

        Assert.Equal(100, source.Read(partialRead));

        source.Reset();

        Assert.Equal(100, source.Read(resetRead));
        Assert.Equal(partialRead, resetRead);
    }

    /// <summary>
    /// Verifies that the sample provider exposes stereo float output and duplicates mono samples.
    /// </summary>
    [Fact]
    public void Read_ExposesStereoFloatFormatAndDuplicatesMonoInput()
    {
        var player = new TestTickPlayer(sampleRate: 48_000, tickCount: 1);
        var emulator = new TestChipEmulator();
        var pipeline = new TickPlaybackPipeline(player, emulator);
        var source = new PipelinePcmSampleSource(pipeline);
        var provider = new RetroSoundSampleProvider(source);
        var buffer = new float[8];

        var samplesRead = provider.Read(buffer, 0, buffer.Length);

        Assert.Equal(48_000, provider.WaveFormat.SampleRate);
        Assert.Equal(2, provider.WaveFormat.Channels);
        Assert.Equal(32, provider.WaveFormat.BitsPerSample);
        Assert.Equal(8, samplesRead);

        Assert.Equal(buffer[0], buffer[1], 6);
        Assert.Equal(buffer[2], buffer[3], 6);
        Assert.Equal(910f / 2048f, buffer[0], 6);
        Assert.Equal(911f / 2048f, buffer[2], 6);
    }

    /// <summary>
    /// Verifies that the sample provider rejects reads that end on a partial stereo frame.
    /// </summary>
    [Fact]
    public void Read_RejectsPartialFramesFromStereoSource()
    {
        var provider = new RetroSoundSampleProvider(new InvalidStereoSampleSource());

        var exception = Assert.Throws<InvalidOperationException>(() => provider.Read(new float[8], 0, 8));

        Assert.Equal("The PCM source returned a partial sample frame.", exception.Message);
    }

    /// <summary>
    /// Verifies that waiting for end of stream completes after the source has been exhausted.
    /// </summary>
    [Fact]
    public async Task WaitForEndOfStreamAsync_CompletesAfterSourceIsExhausted()
    {
        var player = new TestTickPlayer(sampleRate: 48_000, tickCount: 1);
        var emulator = new TestChipEmulator();
        var provider = new RetroSoundSampleProvider(new PipelinePcmSampleSource(new TickPlaybackPipeline(player, emulator)));
        var buffer = new float[2_000];

        var firstRead = provider.Read(buffer, 0, buffer.Length);
        var finalRead = provider.Read(buffer, 0, buffer.Length);

        Assert.True(firstRead > 0);
        Assert.Equal(0, finalRead);
        Assert.True(provider.IsEndOfStream);

        await provider.WaitForEndOfStreamAsync();
    }

    private sealed class InvalidStereoSampleSource : IPcmSampleSource
    {
        public int SampleRate => 48_000;

        public int ChannelCount => 2;

        public void Reset()
        {
        }

        public int Read(Span<float> destination)
        {
            destination[0] = 0.1f;
            destination[1] = 0.2f;
            destination[2] = 0.3f;
            return 3;
        }
    }
}
