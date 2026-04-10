// SPDX-FileCopyrightText: 2026 Anton Shirokov
// SPDX-License-Identifier: MIT

using RetroSound.Core.Audio;
using RetroSound.Core.Playback.Pipelines;
namespace RetroSound.Core.Playback.Sources;

/// <summary>
/// Adapts <see cref="DualChipPlaybackPipeline"/> to a pull-based stereo PCM sample source.
/// One rendered tick is buffered so host integrations can request any sample count without affecting playback
/// determinism.
/// </summary>
internal class DualChipPcmSampleSource : IPcmSampleSource
{
    private readonly DualChipPlaybackPipeline _pipeline;
    private readonly float[] _tickBuffer;
    private int _bufferedValueCount;
    private int _bufferedValueOffset;

    /// <summary>
    /// Initializes a new instance of the <see cref="DualChipPcmSampleSource"/> class.
    /// </summary>
    /// <param name="pipeline">The dual-chip playback pipeline used to render PCM samples.</param>
    public DualChipPcmSampleSource(DualChipPlaybackPipeline pipeline)
    {
        _pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));

        var samplesPerTick = pipeline.Player.Timing.GetSampleFramesPerTick();
        if (samplesPerTick <= 0)
        {
            throw new InvalidOperationException("Playback timing must produce at least one sample per tick.");
        }

        SampleRate = pipeline.Player.Timing.SampleRate;
        ChannelCount = pipeline.ChannelCount;
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
