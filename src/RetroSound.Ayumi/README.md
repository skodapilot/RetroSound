# RetroSound.Ayumi

`RetroSound.Ayumi` is the managed AY/YM synthesis backend for the RetroSound stack.

It takes AY/YM register frames produced by `RetroSound.Core`, runs them through a pure C# ayumi-inspired signal path, and returns floating-point PCM samples that host applications can route to any output layer.


## Reference Projects

The current implementation was informed by this public reference project:  [true-grue/ayumi](https://github.com/true-grue/ayumi)

## What This Project Owns

- AY/YM chip state updates from register frames
- PCM rendering in pure C#
- Integration with `AyYmChipEmulator` through the `IAyYmSampleRendererBackend` contract

## Typical Usage

```csharp
using RetroSound.Ayumi;
using RetroSound.Core.Models;
using RetroSound.Core.Rendering;

using var emulator = new AyYmChipEmulator(
    new AyumiPcmRenderer(),
    new AyYmChipConfiguration(outputChannelCount: 2));
```

In a complete playback pipeline, the emulator is usually fed by an `ITickPlayer` or `DualChipPlayer` from `RetroSound.Core`.

