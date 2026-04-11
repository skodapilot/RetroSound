# RetroSound.Demo.Console

`RetroSound.Demo.Console` is a minimal host application for the RetroSound pipeline.

It is not the core library and not intended as a polished end-user player. Its purpose is to demonstrate how parsing, playback coordination, AY/YM emulation, PCM generation, and Windows audio output fit together in a small runnable sample.

## What This Sample Demonstrates

- Loading TS containers with raw AY register frames
- Loading standalone PT3 modules
- Loading PT3-based TurboSound modules
- Wiring `RetroSound.Core`, `RetroSound.Ayumi`, and `RetroSound.NAudio` into one playback flow
- Basic pause, resume, stop, loop-toggle, and volume controls

## Pipeline Overview

- `RetroSound.Core` parses input files and drives tick playback
- `RetroSound.Ayumi` renders AY/YM register frames into PCM
- `RetroSound.NAudio` sends PCM to Windows audio output

This sample exists to keep those relationships visible and testable during development.

## Run The Sample

```bash
dotnet run --project samples/RetroSound.Demo.Console -- <path-to-input> [sample-rate]
```

Supported inputs:

- TS containers with raw AY register frames
- Standalone PT3 modules
- PT3 TurboSound modules

Example:

```bash
dotnet run --project samples/RetroSound.Demo.Console -- samples/example.pt3 48000
```

Controls while playing:

- `P` or `Space`: pause or resume playback
- `-` or `+`: decrease or increase playback volume
- `L`: toggle loop playback when the current player supports it
- `Ctrl+C`: stop playback early

