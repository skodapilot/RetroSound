// SPDX-FileCopyrightText: 2026 Anton Shirokov
// SPDX-License-Identifier: MIT

namespace RetroSound.NAudio.WaveOut;

/// <summary>
/// Defines the latency settings used by the WaveOut playback adapter.
/// </summary>
public sealed class WaveOutOptions
{
    /// <summary>
    /// Gets or sets the desired output latency in milliseconds.
    /// </summary>
    public int DesiredLatencyMilliseconds
    {
        get;
        init
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Latency must be greater than zero.");
            }

            field = value;
        }
    } = 100;

    /// <summary>
    /// Gets or sets the number of WaveOut buffers.
    /// </summary>
    public int NumberOfBuffers
    {
        get;
        init
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "The number of buffers must be greater than zero.");
            }

            field = value;
        }
    } = 2;
}