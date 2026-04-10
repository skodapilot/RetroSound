// SPDX-FileCopyrightText: 2026 Anton Shirokov
// SPDX-License-Identifier: MIT

namespace RetroSound.Core.Audio;

/// <summary>
/// Exposes a pull-based PCM sample source for host integrations.
/// </summary>
public interface IPcmSampleSource
{
    /// <summary>
    /// Gets the output sample rate in Hertz.
    /// </summary>
    int SampleRate { get; }

    /// <summary>
    /// Gets the number of interleaved PCM channels produced by the source.
    /// </summary>
    int ChannelCount { get; }

    /// <summary>
    /// Resets the source back to the start of playback.
    /// </summary>
    void Reset();

    /// <summary>
    /// Reads interleaved floating-point PCM samples into the destination buffer.
    /// </summary>
    /// <param name="destination">The destination buffer that receives PCM samples.</param>
    /// <returns>The number of samples written to <paramref name="destination"/>.</returns>
    int Read(Span<float> destination);
}
