// SPDX-FileCopyrightText: 2026 Anton Shirokov
// SPDX-License-Identifier: MIT

using RetroSound.Core.Audio;
using RetroSound.Core.Playback.Pipelines;
namespace RetroSound.Core.Playback.Sources;

/// <summary>
/// Adapts the single-chip <see cref="TickPlaybackPipeline"/> to a pull-based PCM sample source.
/// </summary>
internal sealed class PipelinePcmSampleSource : IPcmSampleSource
{
    private readonly TickPlaybackPipeline _pipeline;
    private readonly float[] _tickBuffer;
    private int _bufferedValueCount;
    private int _bufferedValueOffset;

    /// <summary>
    /// Initializes a new instance of the <see cref="PipelinePcmSampleSource"/> class.
    /// </summary>
    /// <param name="pipeline">The tick playback pipeline used to render PCM samples.</param>
    public PipelinePcmSampleSource(TickPlaybackPipeline pipeline)
    {
        _pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));

        // Keep one whole rendered tick in memory so host integrations can pull arbitrary sample counts.
        var samplesPerTick = pipeline.Player.Timing.GetSampleFramesPerTick();

        if (samplesPerTick <= 0)
        {
            throw new InvalidOperationException("Playback timing must produce at least one sample per tick.");
        }

        if (pipeline.Emulator.ChannelCount <= 0)
        {
            throw new InvalidOperationException("The emulator must expose at least one PCM channel.");
        }

        SampleRate = pipeline.Player.Timing.SampleRate;
        ChannelCount = pipeline.Emulator.ChannelCount;
        _tickBuffer = new float[samplesPerTick * ChannelCount];
    }

    /// <summary>
    /// Gets the output sample rate in Hertz.
    /// </summary>
    public int SampleRate { get; }

    /// <summary>
    /// Gets the number of interleaved PCM channels produced by the source.
    /// </summary>
    public int ChannelCount { get; }

    /// <summary>
    /// Resets the source and the underlying playback pipeline.
    /// </summary>
    public void Reset()
    {
        _pipeline.Reset();
        ResetBufferedTickState();
    }

    /// <summary>
    /// Reads interleaved floating-point PCM samples into the destination buffer.
    /// </summary>
    /// <param name="destination">The destination buffer that receives PCM samples.</param>
    /// <returns>The number of samples written to <paramref name="destination"/>.</returns>
    public int Read(Span<float> destination)
    {
        var totalSamplesWritten = 0;

        while (!destination.IsEmpty)
        {
            if (_bufferedValueOffset >= _bufferedValueCount && !TryFillTickBuffer())
            {
                break;
            }

            var availableValueCount = _bufferedValueCount - _bufferedValueOffset;
            var valuesToCopy = Math.Min(availableValueCount, destination.Length);

            _tickBuffer.AsSpan(_bufferedValueOffset, valuesToCopy).CopyTo(destination);

            destination = destination[valuesToCopy..];
            totalSamplesWritten += valuesToCopy;
            _bufferedValueOffset += valuesToCopy;
        }

        return totalSamplesWritten;
    }

    private bool TryFillTickBuffer()
    {
        if (!_pipeline.TryRenderNextTick(_tickBuffer, out var sampleFramesWritten))
        {
            ResetBufferedTickState();
            return false;
        }

        _bufferedValueOffset = 0;
        _bufferedValueCount = sampleFramesWritten * ChannelCount;
        return _bufferedValueCount > 0;
    }

    private void ResetBufferedTickState()
    {
        _bufferedValueCount = 0;
        _bufferedValueOffset = 0;
    }
}
