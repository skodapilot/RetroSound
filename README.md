# RetroSound

RetroSound is a public .NET library for loading retro AY/YM tracker music data, including TurboSound containers and PT3 modules, producing AY/YM register frames, emulating audio generation, and exposing PCM audio for host applications.

The current packages multi-target .NET 8, .NET 9, and .NET 10 so host applications can integrate the library without requiring the newest runtime.

## Installation

Installation is available through NuGet packages:

- [RetroSound.Core](https://www.nuget.org/packages/RetroSound.Core/)
- [RetroSound.Ayumi](https://www.nuget.org/packages/RetroSound.Ayumi/)
- [RetroSound.NAudio](https://www.nuget.org/packages/RetroSound.NAudio/)

The codebase was originally developed for integration into About pages in desktop applications, so it is not intended to be a player in the usual end-user sense. The primary goal is embeddable playback infrastructure for host applications rather than a full standalone listening experience.

## Architecture

The solution is organized into small, focused layers:

- `RetroSound.Core` for format parsing, tracker playback logic, and core abstractions.
- `RetroSound.Ayumi` for a pure C# ayumi-inspired AY/YM backend.
- `RetroSound.NAudio` for Windows-oriented audio output integration built on top of NAudio.
- `RetroSound.Demo.Console` for a minimal host application used during development.
- `RetroSound.Tests` for unit tests covering the core library.

## Development Status

Current playback paths:

- TS containers whose payloads already contain 14-byte AY register frames.
- Standalone PT3 modules parsed into a dedicated PT3 tick player.

The playback pipeline intentionally keeps input parsing, tracker state progression, AY/YM register emission, chip emulation, and host audio output separated so new tracker formats can reuse the same backend.

## Reference Projects

The current implementation was informed by these public reference projects:

- PT3 playback behavior: [Volutar/pt3player](https://github.com/Volutar/pt3player)
- AY/YM synthesis approach: [true-grue/ayumi](https://github.com/true-grue/ayumi)

The sample file `samples/example.pt3` is `Hibernation` by MMCM and is available on ZXArt:
[zxart.ee/eng/authors/m/mmcm1/hibernation](https://zxart.ee/eng/authors/m/mmcm1/hibernation/)

## Minimal PT3 Usage

```csharp
using RetroSound.Ayumi;
using RetroSound.Core.Formats.Pt3;
using RetroSound.Core.Models;
using RetroSound.Core.Playback;
using RetroSound.NAudio;

var module = new Pt3ModuleLoader().LoadFromFile("music.pt3");
var player = new Pt3Player(module, sampleRate: 48_000, stopAfterOrderList: true);

using var emulator = new AyYmChipEmulator(
    new AyumiPcmRenderer(),
    new AyYmChipConfiguration(outputChannelCount: 2));

var provider = new RetroSoundSampleProvider(player, emulator);
using var playback = new WaveOutPlayer(provider, new WaveOutOptions());

playback.Start();
await playback.WaitForPlaybackStoppedAsync();
```

## PT3 Notes

The current PT3 player focuses on a safe, testable baseline:

- PT3 parsing is separate from playback state.
- PT3 playback reuses the existing `ITickPlayer`, AY register frame, emulator, and NAudio paths.
- Invalid PT3 headers, pointer tables, and truncated pattern/sample data produce explicit format exceptions.

Current PT3 limitations:

- Playback currently covers a conservative PT3 command subset intended for basic module support.
- Tone period generation currently uses one shared equal-temperament table instead of version-specific PT3 lookup tables.
- Advanced PT3 effects are parsed conservatively and are not yet rendered musically.
- Restart-position looping is supported and can be toggled at runtime through `Pt3Player.IsLoopingEnabled`.

## Managed AY/YM Backend

The ayumi-oriented renderer lives in `RetroSound.Ayumi` and stays behind the `IAyYmSampleRendererBackend` contract used by `AyYmChipEmulator`.

This keeps the core playback pipeline free from platform-specific bridging code while still isolating the synthesis backend from parsing, tracker logic, and audio output integration.
