// SPDX-FileCopyrightText: 2026 Anton Shirokov
// SPDX-License-Identifier: MIT

using System.Buffers.Binary;
using RetroSound.Core.Formats.Pt3;
using RetroSound.Core.Models;
using RetroSound.Core.Playback.Pt3.Internal;
using RetroSound.Core.Playback.Tracker.Internal;
namespace RetroSound.Core.Playback;

/// <summary>
/// Plays a parsed PT3 module and emits one AY/YM register frame per logical 50 Hz tick.
/// </summary>
/// <remarks>
/// This player intentionally mirrors the PT3 tick model used by the classic `pt3player.c` reference implementation so
/// that row timing, effect progression, and AY/YM register writes stay close to original tracker playback.
/// Reference project: https://github.com/Volutar/pt3player.
/// </remarks>
public sealed class Pt3Player : ITickPlayer, ILoopingPlaybackController
{
    private const int DefaultTicksPerSecond = 50;
    private const int MaxSampleIndex = 31;
    private const int MaxOrnamentIndex = 15;
    private readonly Pt3Module _module;
    private readonly Pt3ChannelPlaybackState[] _channelStates;
    private readonly Pt3PatternPlaybackCursor _patternCursor;
    private readonly Pt3Sample?[] _samplesByIndex = new Pt3Sample?[MaxSampleIndex + 1];
    private readonly Pt3Ornament?[] _ornamentsByIndex = new Pt3Ornament?[MaxOrnamentIndex + 1];
    private readonly TrackerChipState _chipState;
    private readonly Pt3Ornament _defaultOrnament = new(0, 0, 0, [(sbyte)0]);
    private readonly Pt3Sample _defaultSample = new(0, 0, 0, [new Pt3SampleStep(0, 0, 0)]);
    private readonly int _moduleVersion;
    private int _delayCounter;
    private int _currentTempo;
    private int _noiseBase;
    private int _addToNoise;
    private int _envelopeBase;
    private int _currentEnvelopeSlide;
    private int _envelopeSlideAdd;
    private int _currentEnvelopeDelay;
    private int _envelopeDelay;
    private bool _isEndOfStream;

    /// <summary>
    /// Initializes a new instance of the <see cref="Pt3Player"/> class.
    /// </summary>
    /// <param name="module">The parsed PT3 module.</param>
    /// <param name="sampleRate">The target PCM sample rate paired with the emitted 50 Hz timing.</param>
    /// <param name="stopAfterOrderList">When set to <see langword="true"/>, playback stops after the last order entry instead of looping to the restart position.</param>
    public Pt3Player(Pt3Module module, int sampleRate, bool stopAfterOrderList = false)
    {
        _module = module ?? throw new ArgumentNullException(nameof(module));
        IsLoopingEnabled = !stopAfterOrderList;
        foreach (var sample in module.Samples)
        {
            if ((uint)sample.Index <= MaxSampleIndex)
                _samplesByIndex[sample.Index] = sample;
        }

        foreach (var ornament in module.Ornaments)
        {
            if ((uint)ornament.Index <= MaxOrnamentIndex)
                _ornamentsByIndex[ornament.Index] = ornament;
        }

        _chipState = new TrackerChipState();
        _moduleVersion = ParseVersion(module.Metadata.Version);
        _channelStates =
        [
            new Pt3ChannelPlaybackState("A", 0),
            new Pt3ChannelPlaybackState("B", 1),
            new Pt3ChannelPlaybackState("C", 2),
        ];
        _patternCursor = new Pt3PatternPlaybackCursor(module, _channelStates);
        Timing = new PlaybackTiming(DefaultTicksPerSecond, sampleRate);

        Reset();
    }

    /// <summary>
    /// Gets the playback timing used by the player.
    /// </summary>
    public PlaybackTiming Timing { get; }

    /// <summary>
    /// Gets a value indicating whether PT3 playback supports restart-position looping.
    /// </summary>
    public bool SupportsLooping => true;

    /// <summary>
    /// Gets or sets a value indicating whether playback loops to the module restart position.
    /// </summary>
    public bool IsLoopingEnabled { get; set; }

    /// <summary>
    /// Gets a value indicating whether the end of the module stream has been reached.
    /// </summary>
    public bool IsEndOfStream => _isEndOfStream;

    /// <summary>
    /// Resets playback back to the first pattern and first row.
    /// </summary>
    public void Reset()
    {
        _chipState.Reset();
        _currentTempo = _module.Tempo;
        _delayCounter = 1;
        _noiseBase = 0;
        _addToNoise = 0;
        _envelopeBase = 0;
        _currentEnvelopeSlide = 0;
        _envelopeSlideAdd = 0;
        _currentEnvelopeDelay = 0;
        _envelopeDelay = 0;
        _isEndOfStream = false;

        foreach (var channelState in _channelStates)
        {
            channelState.ResetPlaybackState();
        }

        _patternCursor.Reset();
    }

    /// <summary>
    /// Advances playback by one logical tick and emits the next AY/YM register frame.
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

        _delayCounter--;
        if (_delayCounter == 0)
        {
            if (!AdvanceRows())
            {
                frame = null!;
                return false;
            }

            _delayCounter = _currentTempo;
        }

        ApplyTickState();
        frame = AyRegisterFrameAssembler.Assemble(_chipState);
        AdvanceTickState();
        return true;
    }

    private bool AdvanceRows()
    {
        var channelA = _channelStates[0];
        channelA.RemainingRowsUntilParse--;
        if (channelA.RemainingRowsUntilParse == 0)
        {
            if (IsPatternEndMarker(channelA))
            {
                if (!IsLoopingEnabled && _patternCursor.IsAtLastOrder)
                {
                    _isEndOfStream = true;
                    return false;
                }

                _patternCursor.AdvancePattern();
                _noiseBase = 0;
            }

            ParseNextRow(channelA);
        }

        for (var channelIndex = 1; channelIndex < _channelStates.Length; channelIndex++)
        {
            var state = _channelStates[channelIndex];
            state.RemainingRowsUntilParse--;
            if (state.RemainingRowsUntilParse == 0)
            {
                ParseNextRow(state);
            }
        }

        return true;
    }

    private bool IsPatternEndMarker(Pt3ChannelPlaybackState state)
    {
        var stream = state.Pattern.CommandStream.Span;
        return state.StreamPosition >= stream.Length || stream[state.StreamPosition] == 0x00;
    }

    private void ParseNextRow(Pt3ChannelPlaybackState state)
    {
        var stream = state.Pattern.CommandStream.Span;
        if (state.StreamPosition >= stream.Length)
        {
            state.RemainingRowsUntilParse = state.RowsPerNote;
            return;
        }

        Span<byte> pendingEffectsBuffer = stackalloc byte[16];
        var pendingEffectCount = 0;
        var previousNote = state.NoteIndex ?? 0;
        var previousSliding = state.CurrentToneSliding;

        while (state.StreamPosition < stream.Length)
        {
            var command = stream[state.StreamPosition++];
            if (command == 0x00)
            {
                state.RemainingRowsUntilParse = state.RowsPerNote;
                return;
            }

            if (command is >= 0x01 and <= 0x0F)
            {
                if (pendingEffectCount < pendingEffectsBuffer.Length)
                    pendingEffectsBuffer[pendingEffectCount++] = command;
                continue;
            }

            switch (command)
            {
                case >= 0x10 and <= 0x1F:
                    ParseEnvelopeAndSampleCommand(state, command, stream);
                    continue;
                case >= 0x20 and <= 0x3F:
                    _noiseBase = command - 0x20;
                    continue;
                case >= 0x40 and <= 0x4F:
                    state.OrnamentIndex = command & 0x0F;
                    state.OrnamentStepIndex = 0;
                    continue;
                case >= 0x50 and <= 0xAF:
                    state.StartNote(command - 0x50);
                    ParsePendingEffects(state, pendingEffectsBuffer[..pendingEffectCount], stream, previousNote, previousSliding);
                    state.RemainingRowsUntilParse = state.RowsPerNote;
                    return;
                case 0xB0:
                    state.EnvelopeEnabled = false;
                    state.OrnamentStepIndex = 0;
                    continue;
                case 0xB1:
                    state.RowsPerNote = Math.Max(1, (int)ReadByte(stream, state, "skip value"));
                    continue;
                case >= 0xB2 and <= 0xBF:
                    ApplyEnvelopeCommand(state, command, stream);
                    continue;
                case 0xC0:
                    state.StopNote();
                    ParsePendingEffects(state, pendingEffectsBuffer[..pendingEffectCount], stream, previousNote, previousSliding);
                    state.RemainingRowsUntilParse = state.RowsPerNote;
                    return;
                case >= 0xC1 and <= 0xCF:
                    state.BaseVolume = command - 0xC0;
                    continue;
                case 0xD0:
                    ParsePendingEffects(state, pendingEffectsBuffer[..pendingEffectCount], stream, previousNote, previousSliding);
                    state.RemainingRowsUntilParse = state.RowsPerNote;
                    return;
                case >= 0xD1 and <= 0xEF:
                    state.SampleIndex = command - 0xD0;
                    continue;
                case >= 0xF0 and <= 0xFF:
                    state.OrnamentIndex = command & 0x0F;
                    state.OrnamentStepIndex = 0;
                    state.SampleIndex = ReadByte(stream, state, "sample index") / 2;
                    state.EnvelopeEnabled = false;
                    continue;
                default:
                    throw new Pt3PlaybackException($"PT3 command 0x{command:X2} on channel {state.ChannelName} is unsupported.");
            }
        }

        state.RemainingRowsUntilParse = state.RowsPerNote;
    }

    private void ApplyEnvelopeCommand(Pt3ChannelPlaybackState state, byte command, ReadOnlySpan<byte> stream)
    {
        _chipState.EnvelopeShape = command - 0xB1;
        _chipState.EnvelopeShapeWritten = true;
        _envelopeBase = ReadUInt16BigEndian(stream, state, "envelope period");
        _currentEnvelopeSlide = 0;
        _currentEnvelopeDelay = 0;
        state.EnvelopeEnabled = true;
        state.OrnamentStepIndex = 0;
    }

    private void ParseEnvelopeAndSampleCommand(
        Pt3ChannelPlaybackState state,
        byte command,
        ReadOnlySpan<byte> stream)
    {
        if (command == 0x10)
        {
            state.EnvelopeEnabled = false;
            state.SampleIndex = ReadByte(stream, state, "sample index") / 2;
            state.OrnamentIndex = 0;
            state.OrnamentStepIndex = 0;
            return;
        }

        _chipState.EnvelopeShape = command - 0x10;
        _chipState.EnvelopeShapeWritten = true;
        _envelopeBase = ReadUInt16BigEndian(stream, state, "envelope period");
        _currentEnvelopeSlide = 0;
        _currentEnvelopeDelay = 0;
        state.SampleIndex = ReadByte(stream, state, "sample index") / 2;
        state.EnvelopeEnabled = true;
        state.OrnamentStepIndex = 0;
    }

    private void ParsePendingEffects(
        Pt3ChannelPlaybackState state,
        ReadOnlySpan<byte> pendingEffects,
        ReadOnlySpan<byte> stream,
        int previousNote,
        int previousSliding)
    {
        for (var effectIndex = pendingEffects.Length - 1; effectIndex >= 0; effectIndex--)
        {
            var effect = pendingEffects[effectIndex];
            switch (effect)
            {
                case 0x01:
                    state.ToneSlideDelay = ReadByte(stream, state, "glissando delay");
                    state.ToneSlideCount = state.ToneSlideDelay;
                    state.ToneSlideStep = ReadInt16LittleEndian(stream, state, "glissando delta");
                    state.SimpleGliss = true;
                    state.CurrentOnOff = 0;
                    if (state.ToneSlideCount == 0 && _moduleVersion >= 7)
                    {
                        state.ToneSlideCount = 1;
                    }

                    break;
                case 0x02:
                    state.SimpleGliss = false;
                    state.CurrentOnOff = 0;
                    state.ToneSlideDelay = ReadByte(stream, state, "tone portamento delay");
                    state.ToneSlideCount = state.ToneSlideDelay;
                    _ = ReadUInt16LittleEndian(stream, state, "tone portamento base");
                    state.ToneSlideStep = Math.Abs(ReadInt16LittleEndian(stream, state, "tone portamento step"));
                    state.ToneDelta =
                        Pt3NoteTable.GetTonePeriod(state.NoteIndex ?? previousNote, _module.FrequencyTable, _moduleVersion) -
                        Pt3NoteTable.GetTonePeriod(previousNote, _module.FrequencyTable, _moduleVersion);
                    state.SlideToNote = state.NoteIndex ?? previousNote;
                    state.NoteIndex = previousNote;
                    if (_moduleVersion >= 6)
                    {
                        state.CurrentToneSliding = previousSliding;
                    }

                    if (state.ToneDelta - state.CurrentToneSliding < 0)
                    {
                        state.ToneSlideStep = -state.ToneSlideStep;
                    }

                    break;
                case 0x03:
                    state.SampleStepIndex = ReadByte(stream, state, "sample offset");
                    break;
                case 0x04:
                    state.OrnamentStepIndex = ReadByte(stream, state, "ornament offset");
                    break;
                case 0x05:
                    state.OnOffDelay = ReadByte(stream, state, "vibrato on delay");
                    state.OffOnDelay = ReadByte(stream, state, "vibrato off delay");
                    state.CurrentOnOff = state.OnOffDelay;
                    state.ToneSlideCount = 0;
                    state.CurrentToneSliding = 0;
                    break;
                case 0x06:
                case 0x07:
                    break;
                case 0x08:
                    _envelopeDelay = ReadByte(stream, state, "envelope slide delay");
                    _currentEnvelopeDelay = _envelopeDelay;
                    _envelopeSlideAdd = ReadInt16LittleEndian(stream, state, "envelope slide delta");
                    break;
                case 0x09:
                    var newTempo = ReadByte(stream, state, "speed");
                    if (newTempo == 0)
                    {
                        throw new Pt3PlaybackException("PT3 effect 0x09 cannot set the row speed to zero.");
                    }

                    _currentTempo = newTempo;
                    break;
                case >= 0x0A and <= 0x0F:
                    break;
            }
        }
    }

    private void ApplyTickState()
    {
        _chipState.ChannelStates[0].Reset();
        _chipState.ChannelStates[1].Reset();
        _chipState.ChannelStates[2].Reset();

        var addToEnvelope = 0;
        for (var channelIndex = 0; channelIndex < _channelStates.Length; channelIndex++)
        {
            ApplyChannelState(channelIndex, _channelStates[channelIndex], ref addToEnvelope);
        }

        _chipState.NoisePeriod = (_noiseBase + _addToNoise) & 0x1F;
        _chipState.EnvelopePeriod = unchecked((ushort)(_envelopeBase + addToEnvelope + _currentEnvelopeSlide));
    }

    private void ApplyChannelState(int channelIndex, Pt3ChannelPlaybackState state, ref int addToEnvelope)
    {
        var channel = _chipState.ChannelStates[channelIndex];

        if (!state.NoteIndex.HasValue)
        {
            channel.Volume = 0;
            return;
        }

        if (!state.Enabled)
        {
            channel.TonePeriod = state.TonePeriod;
            channel.Volume = 0;
            channel.ToneEnabled = true;
            channel.NoiseEnabled = true;
            channel.UseEnvelope = false;
            return;
        }

        var sample = ResolveSample(state);
        var ornament = ResolveOrnament(state);
        var sampleStep = sample.Steps[WrapIndex(state.SampleStepIndex, sample.Steps.Count)];
        var ornamentOffset = ornament.ToneOffsets[WrapIndex(state.OrnamentStepIndex, ornament.ToneOffsets.Count)];
        var noteIndex = Math.Clamp(state.NoteIndex.Value + ornamentOffset, 0, 95);
        var sampleTone = sampleStep.ToneOffset + state.ToneAccumulator;
        var tonePeriod = (sampleTone + state.CurrentToneSliding + Pt3NoteTable.GetTonePeriod(noteIndex, _module.FrequencyTable, _moduleVersion)) & 0x0FFF;
        var amplitude = (int)sampleStep.Volume;

        if ((sampleStep.Flags & 0x80) != 0)
        {
            if ((sampleStep.Flags & 0x40) != 0)
            {
                if (state.CurrentAmplitudeSliding < 15)
                {
                    state.CurrentAmplitudeSliding++;
                }
            }
            else if (state.CurrentAmplitudeSliding > -15)
            {
                state.CurrentAmplitudeSliding--;
            }
        }

        amplitude += state.CurrentAmplitudeSliding;
        if (amplitude < 0)
        {
            amplitude = 0;
        }
        else if (amplitude > 15)
        {
            amplitude = 15;
        }

        channel.TonePeriod = tonePeriod;
        state.TonePeriod = tonePeriod;
        channel.Volume = Pt3VolumeTable.Map(state.BaseVolume, amplitude, _moduleVersion);
        channel.ToneEnabled = !sampleStep.ToneMasked;
        channel.NoiseEnabled = !sampleStep.NoiseMasked;
        channel.UseEnvelope = !sampleStep.EnvelopeMasked && state.EnvelopeEnabled;

        if ((sampleStep.MixerAndVolume & 0x80) != 0)
        {
            var envelopeDelta = ComputeEnvelopeDelta(sampleStep.Flags, state.CurrentEnvelopeSliding);
            if ((sampleStep.MixerAndVolume & 0x20) != 0)
            {
                state.CurrentEnvelopeSliding = envelopeDelta;
            }

            addToEnvelope += envelopeDelta;
        }
        else
        {
            _addToNoise = unchecked((byte)(((sampleStep.Flags >> 1) & 0x1F) + state.CurrentNoiseSliding));
            if ((sampleStep.MixerAndVolume & 0x20) != 0)
            {
                state.CurrentNoiseSliding = _addToNoise;
            }
        }
    }

    private void AdvanceTickState()
    {
        foreach (var state in _channelStates)
        {
            if (state.Enabled && state.NoteIndex.HasValue)
            {
                var sample = ResolveSample(state);
                var ornament = ResolveOrnament(state);
                var sampleStep = sample.Steps[WrapIndex(state.SampleStepIndex, sample.Steps.Count)];

                if (sampleStep.AccumulateTone)
                {
                    state.ToneAccumulator += sampleStep.ToneOffset;
                }

                if (state.ToneSlideCount > 0)
                {
                    state.ToneSlideCount--;
                    if (state.ToneSlideCount == 0)
                    {
                        state.CurrentToneSliding += state.ToneSlideStep;
                        state.ToneSlideCount = state.ToneSlideDelay;

                        if (!state.SimpleGliss &&
                            ((state.ToneSlideStep < 0 && state.CurrentToneSliding <= state.ToneDelta) ||
                             (state.ToneSlideStep >= 0 && state.CurrentToneSliding >= state.ToneDelta)))
                        {
                            state.NoteIndex = state.SlideToNote;
                            state.ToneSlideCount = 0;
                            state.CurrentToneSliding = 0;
                        }
                    }
                }

                state.SampleStepIndex = AdvanceLoopedIndex(state.SampleStepIndex, sample.Steps.Count, sample.LoopPosition);
                state.OrnamentStepIndex = AdvanceLoopedIndex(state.OrnamentStepIndex, ornament.ToneOffsets.Count, ornament.LoopPosition);
            }

            if (state.CurrentOnOff > 0)
            {
                state.CurrentOnOff--;
                if (state.CurrentOnOff == 0)
                {
                    state.Enabled = !state.Enabled;
                    state.CurrentOnOff = state.Enabled ? state.OnOffDelay : state.OffOnDelay;
                }
            }
        }

        if (_currentEnvelopeDelay > 0)
        {
            _currentEnvelopeDelay--;
            if (_currentEnvelopeDelay == 0)
            {
                _currentEnvelopeDelay = _envelopeDelay;
                _currentEnvelopeSlide += _envelopeSlideAdd;
            }
        }
    }

    private Pt3Sample ResolveSample(Pt3ChannelPlaybackState state)
    {
        var idx = state.SampleIndex;
        return (uint)idx < (uint)_samplesByIndex.Length ? _samplesByIndex[idx] ?? _defaultSample : _defaultSample;
    }

    private Pt3Ornament ResolveOrnament(Pt3ChannelPlaybackState state)
    {
        var idx = state.OrnamentIndex;
        return (uint)idx < (uint)_ornamentsByIndex.Length ? _ornamentsByIndex[idx] ?? _defaultOrnament : _defaultOrnament;
    }

    private static int ComputeEnvelopeDelta(byte flags, int currentEnvelopeSliding)
    {
        var raw = (flags >> 1) & 0x1F;
        var signedRaw = (flags & 0x20) != 0 ? raw - 0x20 : raw;
        return currentEnvelopeSliding + signedRaw;
    }

    private static int AdvanceLoopedIndex(int index, int count, int loopPosition)
    {
        var nextIndex = index + 1;
        return nextIndex < count ? nextIndex : loopPosition;
    }

    private static int WrapIndex(int index, int count)
    {
        if (count <= 0)
        {
            return 0;
        }

        return index < count ? index : count - 1;
    }

    private static int ParseVersion(string? versionText)
    {
        return int.TryParse(versionText, out var version) ? version : 6;
    }

    private static byte ReadByte(ReadOnlySpan<byte> stream, Pt3ChannelPlaybackState state, string description)
    {
        if (state.StreamPosition >= stream.Length)
        {
            throw new Pt3PlaybackException(
                $"PT3 channel {state.ChannelName} is truncated while reading {description} in pattern stream.");
        }

        return stream[state.StreamPosition++];
    }

    private static int ReadUInt16BigEndian(ReadOnlySpan<byte> stream, Pt3ChannelPlaybackState state, string description)
    {
        if (state.StreamPosition + 2 > stream.Length)
        {
            throw new Pt3PlaybackException(
                $"PT3 channel {state.ChannelName} is truncated while reading {description} in pattern stream.");
        }

        var value = BinaryPrimitives.ReadUInt16BigEndian(stream[state.StreamPosition..(state.StreamPosition + 2)]);
        state.StreamPosition += 2;
        return value;
    }

    private static int ReadUInt16LittleEndian(ReadOnlySpan<byte> stream, Pt3ChannelPlaybackState state, string description)
    {
        if (state.StreamPosition + 2 > stream.Length)
        {
            throw new Pt3PlaybackException(
                $"PT3 channel {state.ChannelName} is truncated while reading {description} in pattern stream.");
        }

        var value = BinaryPrimitives.ReadUInt16LittleEndian(stream[state.StreamPosition..(state.StreamPosition + 2)]);
        state.StreamPosition += 2;
        return value;
    }

    private static short ReadInt16LittleEndian(ReadOnlySpan<byte> stream, Pt3ChannelPlaybackState state, string description)
    {
        if (state.StreamPosition + 2 > stream.Length)
        {
            throw new Pt3PlaybackException(
                $"PT3 channel {state.ChannelName} is truncated while reading {description} in pattern stream.");
        }

        var value = BinaryPrimitives.ReadInt16LittleEndian(stream[state.StreamPosition..(state.StreamPosition + 2)]);
        state.StreamPosition += 2;
        return value;
    }
}
