// SPDX-FileCopyrightText: 2026 Anton Shirokov
// SPDX-License-Identifier: MIT

using RetroSound.Ayumi.Synthesis;
using RetroSound.Core.Models;
using RetroSound.Core.Rendering;

namespace RetroSound.Ayumi;

/// <summary>
/// Renders AY/YM register frames to PCM samples using an ayumi-inspired synthesis path.
/// </summary>
/// <remarks>
/// This renderer keeps the public rendering contract stable while implementing chip state, oversampling, and PCM
/// synthesis in pure C# for library hosts without extra native assets.
/// The signal path is informed by the public ayumi project: https://github.com/true-grue/ayumi.
/// </remarks>
public sealed class AyumiPcmRenderer : IAyYmSampleRendererBackend
{
    private readonly AyumiEngine _engine = new();
    private AyYmChipConfiguration? _configuration;
    private int? _configuredSampleRate;
    private bool _disposed;

    /// <summary>
    /// Gets the number of interleaved PCM channels produced by the backend after initialization.
    /// </summary>
    public int ChannelCount => _configuration?.OutputChannelCount ?? 0;

    /// <summary>
    /// Initializes the backend with a stable AY/YM chip configuration.
    /// </summary>
    /// <param name="configuration">The configuration to apply.</param>
    public void Initialize(AyYmChipConfiguration configuration)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(configuration);

        if (_configuration is not null)
        {
            throw new InvalidOperationException("The Ayumi PCM renderer has already been initialized.");
        }

        _configuration = configuration;
        _configuredSampleRate = null;
        _engine.Reset();
    }

    /// <summary>
    /// Resets the AY/YM synthesis state to its initial configuration.
    /// </summary>
    public void Reset()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        EnsureInitialized();

        _engine.Reset();

        if (_configuredSampleRate is { } sampleRate)
        {
            _engine.Configure(_configuration!.ChipType, _configuration.ChipClockHz, sampleRate);
        }
    }

    /// <summary>
    /// Renders one logical AY/YM tick to floating-point PCM samples.
    /// </summary>
    /// <param name="frame">The AY/YM register frame to submit.</param>
    /// <param name="timing">The playback timing that defines the output sample rate.</param>
    /// <param name="destination">The destination buffer for interleaved PCM samples.</param>
    /// <returns>The number of PCM sample frames written to <paramref name="destination"/>.</returns>
    public int Render(AyRegisterFrame frame, PlaybackTiming timing, Span<float> destination)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(frame);
        EnsureInitialized();

        var expectedSampleFrames = timing.GetSampleFramesPerTick();
        if (expectedSampleFrames <= 0)
        {
            throw new InvalidOperationException("Playback timing must produce at least one sample frame per tick.");
        }

        var expectedSampleValues = checked(expectedSampleFrames * ChannelCount);
        if (destination.Length < expectedSampleValues)
        {
            throw new ArgumentException("Destination buffer is too small for one rendered tick.", nameof(destination));
        }

        EnsureSampleRate(timing.SampleRate);
        ApplyRegisters(frame.Registers.Span, frame.EnvelopeShapeWritten);

        var destinationOffset = 0;
        for (var sampleIndex = 0; sampleIndex < expectedSampleFrames; sampleIndex++)
        {
            var (left, right) = _engine.ProcessSample();

            if (ChannelCount == 1)
            {
                destination[destinationOffset] = (left + right) * 0.5f;
            }
            else
            {
                destination[destinationOffset] = left;
                destination[destinationOffset + 1] = right;
            }

            destinationOffset += ChannelCount;
        }

        return expectedSampleFrames;
    }

    /// <summary>
    /// Releases renderer resources.
    /// </summary>
    public void Dispose()
    {
        _disposed = true;
        _configuration = null;
        _configuredSampleRate = null;
    }

    private void EnsureInitialized()
    {
        if (_configuration is null)
        {
            throw new InvalidOperationException("The Ayumi PCM renderer must be initialized before use.");
        }
    }

    private void EnsureSampleRate(int sampleRate)
    {
        if (_configuredSampleRate == sampleRate)
        {
            return;
        }

        _engine.Configure(_configuration!.ChipType, _configuration.ChipClockHz, sampleRate);
        _configuredSampleRate = sampleRate;
    }

    private void ApplyRegisters(ReadOnlySpan<byte> registers, bool envelopeShapeWritten)
    {
        _engine.SetTone(0, registers[0] | ((registers[1] & 0x0f) << 8));
        _engine.SetTone(1, registers[2] | ((registers[3] & 0x0f) << 8));
        _engine.SetTone(2, registers[4] | ((registers[5] & 0x0f) << 8));
        _engine.SetNoise(registers[6]);

        var mixer = registers[7];
        _engine.SetMixer(0, mixer & 0x01, (mixer >> 3) & 0x01, (registers[8] >> 4) & 0x01);
        _engine.SetMixer(1, (mixer >> 1) & 0x01, (mixer >> 4) & 0x01, (registers[9] >> 4) & 0x01);
        _engine.SetMixer(2, (mixer >> 2) & 0x01, (mixer >> 5) & 0x01, (registers[10] >> 4) & 0x01);

        _engine.SetVolume(0, registers[8]);
        _engine.SetVolume(1, registers[9]);
        _engine.SetVolume(2, registers[10]);
        _engine.SetEnvelope(registers[11] | (registers[12] << 8));

        if (envelopeShapeWritten)
        {
            _engine.SetEnvelopeShape(registers[13]);
        }
    }
}
