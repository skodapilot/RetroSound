// SPDX-FileCopyrightText: 2026 Anton Shirokov
// SPDX-License-Identifier: MIT

namespace RetroSound.NAudio.WaveOut;

/// <summary>
/// Defines the latency settings used by the WaveOut playback adapter.
/// </summary>
public sealed class WaveOutOptions
{
    private int _desiredLatencyMilliseconds = 100;
    private int _numberOfBuffers = 2;

    /// <summary>
    /// Gets or sets the desired output latency in milliseconds.
    /// </summary>
    public int DesiredLatencyMilliseconds
    {
        get => _desiredLatencyMilliseconds;
        init
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Latency must be greater than zero.");
            }

            _desiredLatencyMilliseconds = value;
        }
    }

    /// <summary>
    /// Gets or sets the number of WaveOut buffers.
    /// </summary>
    public int NumberOfBuffers
    {
        get => _numberOfBuffers;
        init
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "The number of buffers must be greater than zero.");
            }

            _numberOfBuffers = value;
        }
    }
}
