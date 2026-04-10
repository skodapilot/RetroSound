// SPDX-FileCopyrightText: 2026 Anton Shirokov
// SPDX-License-Identifier: MIT

using RetroSound.Core.Playback;
using RetroSound.Core.Models;
namespace RetroSound.Core.Rendering;

/// <summary>
/// Adapts a production-oriented AY/YM rendering backend to the stable <see cref="IChipEmulator"/> contract.
/// </summary>
/// <remarks>
/// The core library stays independent from any specific native dependency by delegating the actual synthesis work to
/// <see cref="IAyYmSampleRendererBackend"/>. A future ayumi-based package can implement that backend contract without
/// changing the playback pipeline or exposing interop details through the public API.
/// </remarks>
public sealed class AyYmChipEmulator : IChipEmulator, IDisposable
{
    private readonly IAyYmSampleRendererBackend _backend;
    private readonly AyYmChipConfiguration _configuration;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="AyYmChipEmulator"/> class.
    /// </summary>
    /// <param name="backend">The backend that performs the actual AY/YM rendering work.</param>
    /// <param name="configuration">
    /// The static chip configuration. The sample rate still comes from <see cref="PlaybackTiming"/> on each render call.
    /// </param>
    public AyYmChipEmulator(IAyYmSampleRendererBackend backend, AyYmChipConfiguration? configuration = null)
    {
        _backend = backend ?? throw new ArgumentNullException(nameof(backend));
        _configuration = configuration ?? new AyYmChipConfiguration();

        _backend.Initialize(_configuration);

        if (_backend.ChannelCount != _configuration.OutputChannelCount)
        {
            throw new InvalidOperationException(
                "The AY/YM renderer backend channel count must match the configured output channel count.");
        }
    }

    /// <summary>
    /// Gets the number of interleaved PCM channels produced by the emulator.
    /// </summary>
    public int ChannelCount => _configuration.OutputChannelCount;

    /// <summary>
    /// Resets the backend to its initial chip state.
    /// </summary>
    public void Reset()
    {
        ThrowIfDisposed();
        _backend.Reset();
    }

    /// <summary>
    /// Renders one logical AY/YM tick to floating-point PCM samples.
    /// </summary>
    /// <param name="frame">
    /// The AY/YM register frame to render. The frame must contain the 14 standard registers in hardware order:
    /// R0-R5 tone periods, R6 noise period, R7 mixer, R8-R10 channel amplitudes, R11-R12 envelope period,
    /// and R13 envelope shape.
    /// </param>
    /// <param name="timing">
    /// The playback timing that defines the PCM sample rate and tick duration for the current render call.
    /// </param>
    /// <param name="destination">
    /// The destination buffer for interleaved floating-point PCM samples. Stereo output is always left/right
    /// interleaved when <see cref="ChannelCount"/> is <c>2</c>.
    /// </param>
    /// <returns>The number of PCM sample frames written to <paramref name="destination"/>.</returns>
    public int Render(AyRegisterFrame frame, PlaybackTiming timing, Span<float> destination)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(frame);

        var expectedSampleFrames = timing.GetSampleFramesPerTick();
        if (expectedSampleFrames <= 0)
        {
            throw new InvalidOperationException("Playback timing must produce at least one sample frame per tick.");
        }

        if (destination.Length < expectedSampleFrames * ChannelCount)
        {
            throw new ArgumentException("Destination buffer is too small for one rendered tick.", nameof(destination));
        }

        var sampleFramesWritten = _backend.Render(frame, timing, destination);
        if (sampleFramesWritten != expectedSampleFrames)
        {
            throw new InvalidOperationException(
                "The AY/YM renderer backend must render exactly one tick of audio for each register frame.");
        }

        return sampleFramesWritten;
    }

    /// <summary>
    /// Releases backend resources associated with the emulator.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _backend.Dispose();
        _disposed = true;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(AyYmChipEmulator));
        }
    }
}
