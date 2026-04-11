// SPDX-FileCopyrightText: 2026 Anton Shirokov
// SPDX-License-Identifier: MIT

using NAudio.Wave;
namespace RetroSound.NAudio.WaveOut;

/// <summary>
/// Plays a RetroSound sample provider through <see cref="WaveOutEvent"/>.
/// </summary>
public sealed class WaveOutPlayer : IDisposable
{
    private readonly WaveOutEvent _outputDevice;
    private TaskCompletionSource<bool> _playbackStoppedSource;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="WaveOutPlayer"/> class.
    /// </summary>
    /// <param name="sampleProvider">The sample provider to send to the audio device.</param>
    /// <param name="options">The playback options that control latency.</param>
    public WaveOutPlayer(ISampleProvider sampleProvider, WaveOutOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(sampleProvider);

        var resolvedOptions = options ?? new WaveOutOptions();
        _outputDevice = new WaveOutEvent
        {
            DesiredLatency = resolvedOptions.DesiredLatencyMilliseconds,
            NumberOfBuffers = resolvedOptions.NumberOfBuffers,
        };
        _outputDevice.Volume = resolvedOptions.InitialVolume;

        _playbackStoppedSource = CreatePlaybackStoppedSource();
        _outputDevice.PlaybackStopped += OnPlaybackStopped;
        _outputDevice.Init(sampleProvider);
    }

    /// <summary>
    /// Starts playback.
    /// </summary>
    public void Start()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_outputDevice.PlaybackState == PlaybackState.Stopped)
        {
            _playbackStoppedSource = CreatePlaybackStoppedSource();
        }

        _outputDevice.Play();
    }

    /// <summary>
    /// Pauses playback without resetting the playback position.
    /// </summary>
    public void Pause()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_outputDevice.PlaybackState == PlaybackState.Playing)
        {
            _outputDevice.Pause();
        }
    }

    /// <summary>
    /// Resumes playback after a pause.
    /// </summary>
    public void Resume()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_outputDevice.PlaybackState == PlaybackState.Paused)
        {
            _outputDevice.Play();
        }
    }

    /// <summary>
    /// Stops playback.
    /// </summary>
    public void Stop()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _outputDevice.Stop();
    }

    /// <summary>
    /// Waits for playback to stop.
    /// </summary>
    /// <param name="cancellationToken">A token used to cancel the wait.</param>
    /// <returns>A task that completes when playback stops.</returns>
    public Task WaitForPlaybackStoppedAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _playbackStoppedSource.Task.WaitAsync(cancellationToken);
    }

    /// <summary>
    /// Gets a value indicating whether playback is currently paused.
    /// </summary>
    public bool IsPaused
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _outputDevice.PlaybackState == PlaybackState.Paused;
        }
    }

    /// <summary>
    /// Gets or sets the playback volume in the inclusive range from 0.0 to 1.0.
    /// </summary>
    public float Volume
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _outputDevice.Volume;
        }
        set
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (value is < 0.0f or > 1.0f)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "The volume must be between 0.0 and 1.0.");
            }

            _outputDevice.Volume = value;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _outputDevice.PlaybackStopped -= OnPlaybackStopped;
        _outputDevice.Dispose();
        _playbackStoppedSource.TrySetResult(true);
    }

    private static TaskCompletionSource<bool> CreatePlaybackStoppedSource()
    {
        return new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    private void OnPlaybackStopped(object? sender, StoppedEventArgs e)
    {
        if (e.Exception is not null)
        {
            _playbackStoppedSource.TrySetException(e.Exception);
            return;
        }

        _playbackStoppedSource.TrySetResult(true);
    }
}
