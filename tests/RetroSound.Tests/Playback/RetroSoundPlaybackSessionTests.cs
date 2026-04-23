// SPDX-FileCopyrightText: 2026 Anton Shirokov
// SPDX-License-Identifier: MIT

using RetroSound.Core.Formats.TurboSound;
using RetroSound.Core.Models;
using RetroSound.Core.Playback;
using RetroSound.Core.Rendering;
using RetroSound.NAudio;
using Xunit;

namespace RetroSound.Tests.Playback;

public sealed class RetroSoundPlaybackSessionTests
{
    /// <summary>
    /// Verifies that a standalone PT3 file creates a single-chip playback session and owns one emulator instance.
    /// </summary>
    [Fact]
    public void CreateFromFile_Pt3_CreatesSingleChipSession()
    {
        var emulators = new List<TrackingChipEmulator>();

        using (var session = RetroSoundPlaybackSession.CreateFromFile(
                   GetTestDataPath("minimal-valid.pt3"),
                   sampleRate: 48_000,
                   createChipEmulator: () => CreateTrackingEmulator(emulators),
                   stopAfterOrderList: true))
        {
            Assert.Equal(TurboSoundInputKind.Pt3Module, session.InputKind);
            Assert.Equal("Minimal PT3 Sample", session.Title);
            Assert.NotNull(session.LoopingController);
            Assert.True(session.LoopingController!.SupportsLooping);
            Assert.False(session.LoopingController.IsLoopingEnabled);
            var emulator = Assert.Single(emulators);
            Assert.Equal(48_000, session.SampleProvider.WaveFormat.SampleRate);
            Assert.Equal(2, session.SampleProvider.WaveFormat.Channels);
            Assert.False(emulator.IsDisposed);
        }

        _ = Assert.Single(emulators);
        Assert.All(emulators, emulator => Assert.True(emulator.IsDisposed));
    }

    /// <summary>
    /// Verifies that a PT3 TurboSound file creates a dual-chip playback session and owns both emulator instances.
    /// </summary>
    [Fact]
    public void CreateFromFile_Pt3TurboSound_CreatesDualChipSession()
    {
        var emulators = new List<TrackingChipEmulator>();
        var filePath = CreateTemporaryTurboSoundPt3File();

        try
        {
            using (var session = RetroSoundPlaybackSession.CreateFromFile(
                       filePath,
                       sampleRate: 48_000,
                       createChipEmulator: () => CreateTrackingEmulator(emulators),
                       stopAfterOrderList: true))
            {
                Assert.Equal(TurboSoundInputKind.Pt3TurboSoundModule, session.InputKind);
                Assert.Equal("Minimal PT3 Sample", session.Title);
                Assert.NotNull(session.LoopingController);
                Assert.True(session.LoopingController!.SupportsLooping);
                Assert.False(session.LoopingController.IsLoopingEnabled);
                Assert.Equal(2, emulators.Count);
                Assert.Equal(48_000, session.SampleProvider.WaveFormat.SampleRate);
                Assert.Equal(2, session.SampleProvider.WaveFormat.Channels);
                Assert.All(emulators, emulator => Assert.False(emulator.IsDisposed));
            }

            Assert.Equal(2, emulators.Count);
            Assert.All(emulators, emulator => Assert.True(emulator.IsDisposed));
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    private static TrackingChipEmulator CreateTrackingEmulator(ICollection<TrackingChipEmulator> emulators)
    {
        var emulator = new TrackingChipEmulator();
        emulators.Add(emulator);
        return emulator;
    }

    private static string CreateTemporaryTurboSoundPt3File()
    {
        var pt3Bytes = File.ReadAllBytes(GetTestDataPath("minimal-valid.pt3"));
        var turboSoundBytes = new byte[pt3Bytes.Length * 2];
        Buffer.BlockCopy(pt3Bytes, 0, turboSoundBytes, 0, pt3Bytes.Length);
        Buffer.BlockCopy(pt3Bytes, 0, turboSoundBytes, pt3Bytes.Length, pt3Bytes.Length);

        var filePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.pt3");
        File.WriteAllBytes(filePath, turboSoundBytes);
        return filePath;
    }

    private static string GetTestDataPath(string fileName)
    {
        return Path.Combine(AppContext.BaseDirectory, "TestData", fileName);
    }

    private sealed class TrackingChipEmulator : IChipEmulator, IDisposable
    {
        public int ChannelCount => 2;

        public bool IsDisposed { get; private set; }

        public void Reset()
        {
        }

        public int Render(AyRegisterFrame frame, PlaybackTiming timing, Span<float> destination)
        {
            var sampleFrames = timing.GetSampleFramesPerTick();
            destination[..(sampleFrames * ChannelCount)].Clear();
            return sampleFrames;
        }

        public void Dispose()
        {
            IsDisposed = true;
        }
    }
}
