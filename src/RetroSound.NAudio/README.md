# RetroSound.NAudio

`RetroSound.NAudio` is the Windows-oriented output integration layer for RetroSound.

It bridges RetroSound PCM sources to NAudio by exposing `ISampleProvider` adapters and a small `WaveOutEvent` playback wrapper. This keeps audio device concerns separate from parsing, tracker playback, and AY/YM synthesis.

## What This Project Owns

- Adapting `IPcmSampleSource` to NAudio `ISampleProvider`
- Stereo output normalization for RetroSound PCM streams
- Basic playback hosting through `WaveOutPlayer`
- Playback volume control through `WaveOutPlayer.Volume`

## What This Project Depends On

- `RetroSound.Core` for PCM source and playback abstractions
- A synthesis backend such as `RetroSound.Ayumi` to turn AY/YM register frames into PCM
- `NAudio` for Windows audio output

## Typical Usage

```csharp
using RetroSound.Ayumi;
using RetroSound.Core.Models;
using RetroSound.Core.Rendering;
using RetroSound.NAudio;
using RetroSound.NAudio.WaveOut;

using var session = RetroSoundPlaybackSession.CreateFromFile(
    "music.pt3",
    sampleRate: 48_000,
    createChipEmulator: () => new AyYmChipEmulator(
        new AyumiPcmRenderer(),
        new AyYmChipConfiguration(outputChannelCount: 2)));

using var playback = new WaveOutPlayer(session.SampleProvider, new WaveOutOptions());

playback.Start();
await playback.WaitForPlaybackStoppedAsync();
```
