// SPDX-FileCopyrightText: 2026 Anton Shirokov
// SPDX-License-Identifier: MIT

using NAudio.Wave;
using RetroSound.Core.Audio;
using RetroSound.Core.Playback;
using RetroSound.Core.Rendering;
namespace RetroSound.NAudio;

/// <summary>
/// Exposes an <see cref="IPcmSampleSource"/> as a stereo floating-point NAudio sample provider.
/// </summary>
public sealed class RetroSoundSampleProvider : ISampleProvider
{
    private const int OutputChannelCount = 2;
    private readonly IPcmSampleSource _source;
    private float[] _sourceBuffer;
    private TaskCompletionSource<bool> _endOfStreamSource;

    /// <summary>
    /// Initializes a new instance of the <see cref="RetroSoundSampleProvider"/> class.
    /// </summary>
    /// <param name="source">The PCM source that provides floating-point samples.</param>
    public RetroSoundSampleProvider(IPcmSampleSource source)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));

        if (source.ChannelCount is < 1 or > 2)
        {
            throw new NotSupportedException("Only mono and stereo PCM sources are supported.");
        }

        WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(source.SampleRate, OutputChannelCount);
        _sourceBuffer = Array.Empty<float>();
        _endOfStreamSource = CreateEndOfStreamSource();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RetroSoundSampleProvider"/> class for dual-chip playback.
    /// </summary>
    /// <param name="player">The dual-chip playback coordinator.</param>
    /// <param name="chipAEmulator">The emulator that renders the first AY/YM chip.</param>
    /// <param name="chipBEmulator">The emulator that renders the second AY/YM chip.</param>
    public RetroSoundSampleProvider(
        DualChipPlayer player,
        IChipEmulator chipAEmulator,
        IChipEmulator chipBEmulator)
        : this(CreateDualChipSource(player, chipAEmulator, chipBEmulator))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RetroSoundSampleProvider"/> class for single-chip playback.
    /// </summary>
    /// <param name="player">The single-chip tick player.</param>
    /// <param name="emulator">The emulator that renders the AY/YM chip.</param>
    public RetroSoundSampleProvider(ITickPlayer player, IChipEmulator emulator)
        : this(CreateSingleChipSource(player, emulator))
    {
    }

    /// <summary>
    /// Gets the stereo floating-point wave format exposed to NAudio.
    /// </summary>
    public WaveFormat WaveFormat { get; }

    /// <summary>
    /// Gets a value indicating whether the underlying PCM source has reached the end of the stream.
    /// </summary>
    public bool IsEndOfStream { get; private set; }

    /// <summary>
    /// Waits for the underlying PCM source to reach the end of the stream.
    /// </summary>
    /// <param name="cancellationToken">A token used to cancel the wait.</param>
    /// <returns>A task that completes when the source stops producing samples.</returns>
    public Task WaitForEndOfStreamAsync(CancellationToken cancellationToken = default)
    {
        return _endOfStreamSource.Task.WaitAsync(cancellationToken);
    }

    /// <summary>
    /// Reads stereo floating-point PCM samples into the destination buffer.
    /// </summary>
    /// <param name="buffer">The destination buffer.</param>
    /// <param name="offset">The first destination index to write.</param>
    /// <param name="count">The number of interleaved sample values requested.</param>
    /// <returns>The number of samples written to <paramref name="buffer"/>.</returns>
    public int Read(float[] buffer, int offset, int count)
    {
        ArgumentNullException.ThrowIfNull(buffer);

        if (offset < 0 || offset > buffer.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(offset));
        }

        if (count < 0 || offset + count > buffer.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        if (count == 0)
        {
            return 0;
        }

        var destination = buffer.AsSpan(offset, count);
        var destinationFrameCapacity = destination.Length / OutputChannelCount;
        if (destinationFrameCapacity == 0)
        {
            return 0;
        }

        var requiredSourceSampleValueCount = destinationFrameCapacity * _source.ChannelCount;
        EnsureSourceBufferCapacity(requiredSourceSampleValueCount);

        var sourceValuesRead = _source.Read(_sourceBuffer.AsSpan(0, requiredSourceSampleValueCount));
        if (sourceValuesRead == 0)
        {
            IsEndOfStream = true;
            _endOfStreamSource.TrySetResult(true);
            return 0;
        }

        IsEndOfStream = false;

        if (sourceValuesRead % _source.ChannelCount != 0)
        {
            throw new InvalidOperationException("The PCM source returned a partial sample frame.");
        }

        var sourceFramesRead = sourceValuesRead / _source.ChannelCount;
        var writtenSamples = sourceFramesRead * OutputChannelCount;

        if (_source.ChannelCount == 1)
        {
            var destIdx = 0;
            for (var frameIndex = 0; frameIndex < sourceFramesRead; frameIndex++)
            {
                var sample = _sourceBuffer[frameIndex];
                destination[destIdx] = sample;
                destination[destIdx + 1] = sample;
                destIdx += OutputChannelCount;
            }
        }
        else
        {
            _sourceBuffer.AsSpan(0, writtenSamples).CopyTo(destination);
        }

        return writtenSamples;
    }

    private void EnsureSourceBufferCapacity(int requiredValueCount)
    {
        if (_sourceBuffer.Length < requiredValueCount)
        {
            _sourceBuffer = new float[requiredValueCount];
        }
    }

    private static TaskCompletionSource<bool> CreateEndOfStreamSource()
    {
        return new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    private static IPcmSampleSource CreateDualChipSource(
        DualChipPlayer player,
        IChipEmulator chipAEmulator,
        IChipEmulator chipBEmulator)
    {
        return PcmSampleSourceFactory.Create(player, chipAEmulator, chipBEmulator);
    }

    private static IPcmSampleSource CreateSingleChipSource(ITickPlayer player, IChipEmulator emulator)
    {
        return PcmSampleSourceFactory.Create(player, emulator);
    }
}
