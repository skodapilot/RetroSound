// SPDX-FileCopyrightText: 2026 Anton Shirokov
// SPDX-License-Identifier: MIT

using RetroSound.Core.Formats.Pt3;
namespace RetroSound.Core.Playback.Pt3.Internal;

internal sealed class Pt3ChannelPlaybackState(string channelName, int channelIndex)
{

    public string ChannelName { get; } = channelName;

    public int ChannelIndex { get; } = channelIndex;

    public Pt3ChannelPattern Pattern { get; private set; } = null!;

    public int StreamPosition { get; set; }

    public int? NoteIndex { get; set; }

    public bool Enabled { get; set; }

    public int BaseVolume { get; set; } = 15;

    public int SampleIndex { get; set; } = 1;

    public int OrnamentIndex { get; set; }

    public int SampleStepIndex { get; set; }

    public int OrnamentStepIndex { get; set; }

    public bool EnvelopeEnabled { get; set; }

    public int RowsPerNote { get; set; } = 1;

    public int RemainingRowsUntilParse { get; set; } = 1;

    public int CurrentAmplitudeSliding { get; set; }

    public int CurrentNoiseSliding { get; set; }

    public int CurrentEnvelopeSliding { get; set; }

    public int ToneSlideCount { get; set; }

    public int CurrentOnOff { get; set; }

    public int OnOffDelay { get; set; }

    public int OffOnDelay { get; set; }

    public int ToneSlideDelay { get; set; }

    public int CurrentToneSliding { get; set; }

    public int ToneAccumulator { get; set; }

    public int TonePeriod { get; set; }

    public int ToneSlideStep { get; set; }

    public int ToneDelta { get; set; }

    public int SlideToNote { get; set; }

    public bool SimpleGliss { get; set; }

    public void AssignPattern(Pt3ChannelPattern pattern)
    {
        Pattern = pattern ?? throw new ArgumentNullException(nameof(pattern));
        StreamPosition = 0;
    }

    public void ResetPlaybackState()
    {
        NoteIndex = null;
        Enabled = false;
        BaseVolume = 15;
        SampleIndex = 1;
        OrnamentIndex = 0;
        SampleStepIndex = 0;
        OrnamentStepIndex = 0;
        EnvelopeEnabled = false;
        RowsPerNote = 1;
        RemainingRowsUntilParse = 1;
        CurrentAmplitudeSliding = 0;
        CurrentNoiseSliding = 0;
        CurrentEnvelopeSliding = 0;
        ToneSlideCount = 0;
        CurrentOnOff = 0;
        OnOffDelay = 0;
        OffOnDelay = 0;
        ToneSlideDelay = 0;
        CurrentToneSliding = 0;
        ToneAccumulator = 0;
        TonePeriod = 0;
        ToneSlideStep = 0;
        ToneDelta = 0;
        SlideToNote = 0;
        SimpleGliss = false;
    }

    public void StartNote(int noteIndex)
    {
        NoteIndex = noteIndex;
        Enabled = true;
        SampleStepIndex = 0;
        OrnamentStepIndex = 0;
        CurrentAmplitudeSliding = 0;
        CurrentNoiseSliding = 0;
        CurrentEnvelopeSliding = 0;
        ToneSlideCount = 0;
        CurrentToneSliding = 0;
        ToneAccumulator = 0;
        TonePeriod = 0;
        CurrentOnOff = 0;
        ToneSlideStep = 0;
        ToneDelta = 0;
        SlideToNote = noteIndex;
        SimpleGliss = false;
    }

    public void StopNote()
    {
        Enabled = false;
        SampleStepIndex = 0;
        CurrentAmplitudeSliding = 0;
        CurrentNoiseSliding = 0;
        CurrentEnvelopeSliding = 0;
        OrnamentStepIndex = 0;
        ToneSlideCount = 0;
        CurrentToneSliding = 0;
        ToneAccumulator = 0;
        CurrentOnOff = 0;
        ToneSlideStep = 0;
        ToneDelta = 0;
        SlideToNote = 0;
        SimpleGliss = false;
    }
}