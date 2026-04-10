// SPDX-FileCopyrightText: 2026 Anton Shirokov
// SPDX-License-Identifier: MIT

using RetroSound.Core.Models;
using RetroSound.Core.Playback;
using RetroSound.Core.Playback.Tracker.Internal;
namespace RetroSound.Core.Playback.Tracker;

/// <summary>
/// Plays one single-chip tracker module and emits one AY/YM register frame per 50 Hz tick.
/// </summary>
internal sealed class SingleChipTrackerPlayer : ITickPlayer
{
    private readonly TrackerPlaybackCursor _cursor;
    private readonly TrackerChipState _chipState;

    /// <summary>
    /// Initializes a new instance of the <see cref="SingleChipTrackerPlayer"/> class.
    /// </summary>
    /// <param name="module">The single-chip tracker module to play.</param>
    /// <param name="sampleRate">The target PCM output sample rate paired with the emitted 50 Hz timing.</param>
    public SingleChipTrackerPlayer(SingleChipTrackerModule module, int sampleRate)
    {
        module = module ?? throw new ArgumentNullException(nameof(module));
        Timing = new PlaybackTiming(50, sampleRate);
        _cursor = new TrackerPlaybackCursor(module);
        _chipState = new TrackerChipState();

        Reset();
    }

    /// <summary>
    /// Gets the playback timing used by the player.
    /// </summary>
    public PlaybackTiming Timing { get; }

    /// <summary>
    /// Gets a value indicating whether the end of the module stream has been reached.
    /// </summary>
    public bool IsEndOfStream => _cursor.IsEndOfStream;

    /// <summary>
    /// Resets playback back to the first order, first row, and first tick.
    /// </summary>
    public void Reset()
    {
        _chipState.Reset();
        _cursor.Reset();
    }

    /// <summary>
    /// Advances playback by one logical tick and emits the resulting full AY/YM register frame.
    /// </summary>
    /// <param name="frame">When this method returns <see langword="true"/>, contains the next AY/YM register frame.</param>
    /// <returns><see langword="true"/> when a frame was produced; otherwise, <see langword="false"/>.</returns>
    public bool TryAdvance(out AyRegisterFrame frame)
    {
        if (IsEndOfStream)
        {
            frame = null!;
            return false;
        }

        if (_cursor.IsAtRowStart)
        {
            ApplyRowCommands(_cursor.CurrentPatternRow);
        }

        frame = AyRegisterFrameAssembler.Assemble(_chipState);

        // Effects run after the current frame is emitted so the next tick observes the updated state.
        foreach (var channelState in _chipState.ChannelStates)
        {
            TrackerEffectProcessor.Apply(channelState);
        }

        _cursor.Advance();
        return true;
    }

    private void ApplyRowCommands(SingleChipPatternRow row)
    {
        ApplyChannelCommand(_chipState.ChannelStates[0], row.ChannelA);
        ApplyChannelCommand(_chipState.ChannelStates[1], row.ChannelB);
        ApplyChannelCommand(_chipState.ChannelStates[2], row.ChannelC);

        if (row.NoisePeriod.HasValue)
        {
            _chipState.NoisePeriod = row.NoisePeriod.Value;
        }

        if (row.EnvelopePeriod.HasValue)
        {
            _chipState.EnvelopePeriod = row.EnvelopePeriod.Value;
        }

        if (row.EnvelopeShape.HasValue)
        {
            _chipState.EnvelopeShape = row.EnvelopeShape.Value;
            _chipState.EnvelopeShapeWritten = true;
        }
    }

    private static void ApplyChannelCommand(TrackerChannelState state, SingleChipChannelCommand command)
    {
        if (command.TonePeriod.HasValue)
        {
            state.TonePeriod = command.TonePeriod.Value;
        }

        if (command.Volume.HasValue)
        {
            state.Volume = command.Volume.Value;
        }

        if (command.ToneEnabled.HasValue)
        {
            state.ToneEnabled = command.ToneEnabled.Value;
        }

        if (command.NoiseEnabled.HasValue)
        {
            state.NoiseEnabled = command.NoiseEnabled.Value;
        }

        if (command.UseEnvelope.HasValue)
        {
            state.UseEnvelope = command.UseEnvelope.Value;
        }

        state.Effect = command.Effect;
    }
}
