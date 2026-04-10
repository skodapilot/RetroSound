# RetroSound.Core Architecture

`RetroSound.Core` defines the contracts that connect the playback pipeline without committing to any file format parser, tracker engine, or chip emulator implementation.

The intended layering is:

1. Container parsing
   Reads bytes and returns format-specific models such as `TurboSoundContainer` or `Pt3Module` without mixing parsing with playback state.
2. Tracker or player logic
   Consumes parsed container data and produces one `AyRegisterFrame` per logical music tick through `ITickPlayer`.
3. AY/YM emulation
   Consumes register frames and renders PCM samples through `IChipEmulator`.
4. Output integration
   Adapters outside the core library forward rendered PCM data to host-specific systems such as NAudio.

The important boundary is between container parsing and playback. Parsers should only expose container structure and raw module payloads. `ITickPlayer` owns musical state progression and timing, while `IChipEmulator` turns a single tick-sized register frame into PCM data using `PlaybackTiming`.

For production integration, `AyYmChipEmulator` keeps the playback-facing contract stable and delegates the synthesis work to `IAyYmSampleRendererBackend`. The `RetroSound.Ayumi` package is the first concrete implementation layer for that seam. It keeps the synthesis state and oversampled signal path in a focused C# assembly so the playback pipeline can evolve without taking a dependency on native bridge packaging.

This keeps the core library small, testable, and open to multiple parser, player, and emulator implementations.

The first production-oriented player in the core library is `SingleChipTrackerPlayer`. It keeps pattern/order progression, mutable channel state, per-tick effect application, and AY/YM frame assembly in separate responsibilities so one player instance stays easy to test.

`DualChipPlayer` composes two `ITickPlayer` instances and advances them in lockstep, returning a `DualChipPlaybackFrame` that exposes one `AyRegisterFrame` per chip. `DualChipPlaybackPipeline` then renders that shared tick through two `IChipEmulator` instances and mixes the chip outputs into one stereo PCM block. This keeps dual-chip orchestration in the playback layer without mixing it into parsing or host-specific output integration.

PT3 support follows the same split. `Pt3ModuleLoader` parses the PT3 container structures, while `Pt3Player` interprets PT3 pattern bytecode and emits one `AyRegisterFrame` per tick through `ITickPlayer`. That lets PT3 reuse the same AY/YM emulation and PCM output path already used by the rest of the library.

The PT3 parsing and playback behavior is guided by the public `pt3player` reference implementation from Volutar: https://github.com/Volutar/pt3player. The AY/YM synthesis path in `RetroSound.Ayumi` is similarly informed by the public `ayumi` project from true-grue: https://github.com/true-grue/ayumi.

To reduce integration risk before real TS/PT3 parsing and AY/YM emulation are added, the core library also includes deterministic fake implementations and a small pipeline coordinator. These fake components exercise the same contracts as production implementations, which lets unit tests validate tick progression, frame handoff, and PCM rendering flow without depending on audio fidelity.
