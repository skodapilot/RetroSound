// SPDX-FileCopyrightText: 2026 Anton Shirokov
// SPDX-License-Identifier: MIT

using RetroSound.Core.Rendering;
using RetroSound.Core.Models;
namespace RetroSound.Core.Rendering;

/// <summary>
/// Defines the backend boundary used by <see cref="AyYmChipEmulator"/>.
/// </summary>
/// <remarks>
/// This contract is intentionally managed and backend-agnostic so native or managed AY/YM engines can plug into
/// the core pipeline without exposing interop-specific handles, callbacks, or transport details.
/// </remarks>
public interface IAyYmSampleRendererBackend : IDisposable
{
    /// <summary>
    /// Gets the number of interleaved PCM channels produced by the backend after initialization.
    /// A value of <c>1</c> means mono output, and a value of <c>2</c> means stereo output ordered as left then right.
    /// </summary>
    int ChannelCount { get; }

    /// <summary>
    /// Initializes the backend for AY/YM sample rendering.
    /// </summary>
    /// <param name="configuration">The chip configuration that remains stable for the backend lifetime.</param>
    void Initialize(AyYmChipConfiguration configuration);

    /// <summary>
    /// Resets the backend to its initial chip state.
    /// </summary>
    void Reset();

    /// <summary>
    /// Renders one logical AY/YM register frame to floating-point PCM samples.
    /// </summary>
    /// <param name="frame">
    /// The AY/YM register frame to render. The frame must contain the 14 standard registers in hardware order:
    /// R0-R5 tone periods, R6 noise period, R7 mixer, R8-R10 channel amplitudes, R11-R12 envelope period,
    /// and R13 envelope shape.
    /// </param>
    /// <param name="timing">
    /// The playback timing that supplies the output sample rate and the duration of the current logical tick.
    /// </param>
    /// <param name="destination">
    /// The destination buffer for interleaved floating-point PCM samples. Backends are expected to write one full
    /// tick of audio as <c>Round(timing.SamplesPerTick)</c> sample frames, multiplied by <see cref="ChannelCount"/>.
    /// </param>
    /// <returns>The number of PCM sample frames written to <paramref name="destination"/>.</returns>
    int Render(AyRegisterFrame frame, PlaybackTiming timing, Span<float> destination);
}
