// SPDX-FileCopyrightText: 2026 Anton Shirokov
// SPDX-License-Identifier: MIT

using RetroSound.Core.Models;
namespace RetroSound.Core.Rendering;

/// <summary>
/// Consumes AY/YM register frames and renders PCM samples for a logical playback tick.
/// </summary>
public interface IChipEmulator
{
    /// <summary>
    /// Gets the number of interleaved PCM channels produced by the emulator.
    /// A value of <c>1</c> means mono output, and a value of <c>2</c> means stereo output
    /// ordered as left then right.
    /// </summary>
    int ChannelCount { get; }

    /// <summary>
    /// Resets the emulator state.
    /// </summary>
    void Reset();

    /// <summary>
    /// Renders PCM sample frames for one logical playback tick.
    /// </summary>
    /// <param name="frame">
    /// The AY/YM register frame to render. The frame must contain the 14 standard registers in hardware order:
    /// R0-R5 tone periods, R6 noise period, R7 mixer, R8-R10 channel amplitudes, R11-R12 envelope period,
    /// and R13 envelope shape.
    /// </param>
    /// <param name="timing">
    /// The playback timing that defines the tick duration and output sample rate. Implementations should treat
    /// <see cref="PlaybackTiming.SampleRate"/> as the PCM sample rate for the rendered tick.
    /// </param>
    /// <param name="destination">
    /// The destination buffer for interleaved floating-point PCM samples. Implementations must write one full
    /// logical tick of audio as <c>Round(timing.SamplesPerTick)</c> sample frames, multiplied by
    /// <see cref="ChannelCount"/>.
    /// </param>
    /// <returns>The number of PCM sample frames written to <paramref name="destination"/>.</returns>
    int Render(AyRegisterFrame frame, PlaybackTiming timing, Span<float> destination);
}
