// SPDX-FileCopyrightText: 2026 Anton Shirokov
// SPDX-License-Identifier: MIT

using RetroSound.Core.Formats.Pt3;
using RetroSound.Core.Formats.TurboSound;
using RetroSound.Core.Models;
using RetroSound.Core.Playback;
using RetroSound.Core.Rendering;

namespace RetroSound.NAudio;

/// <summary>
/// Owns a ready-to-play RetroSound playback graph created from a supported input file.
/// </summary>
/// <remarks>
/// This facade keeps PT3 and PT3 TurboSound player composition out of host code while preserving the existing low-level
/// parsing and playback building blocks for callers that need more control.
/// </remarks>
public sealed class RetroSoundPlaybackSession : IDisposable
{
    private readonly IDisposable[] _ownedResources;
    private bool _disposed;

    private RetroSoundPlaybackSession(
        RetroSoundSampleProvider sampleProvider,
        TurboSoundInputKind inputKind,
        string? title,
        ILoopingPlaybackController? loopingController,
        IDisposable[] ownedResources)
    {
        SampleProvider = sampleProvider ?? throw new ArgumentNullException(nameof(sampleProvider));
        InputKind = inputKind;
        Title = title;
        LoopingController = loopingController;
        _ownedResources = ownedResources ?? throw new ArgumentNullException(nameof(ownedResources));
    }

    /// <summary>
    /// Gets the NAudio sample provider ready to be passed to a playback host.
    /// </summary>
    public RetroSoundSampleProvider SampleProvider { get; }

    /// <summary>
    /// Gets the detected input kind used to build the playback graph.
    /// </summary>
    public TurboSoundInputKind InputKind { get; }

    /// <summary>
    /// Gets the best available display title for the loaded input.
    /// </summary>
    public string? Title { get; }

    /// <summary>
    /// Gets optional loop playback controls when the loaded input supports them.
    /// </summary>
    public ILoopingPlaybackController? LoopingController { get; }

    /// <summary>
    /// Creates a playback session from a supported file and the provided chip emulator factory.
    /// </summary>
    /// <param name="filePath">The input file path.</param>
    /// <param name="sampleRate">The target PCM sample rate.</param>
    /// <param name="createChipEmulator">
    /// A factory that returns a fresh AY/YM emulator instance each time it is called. Dual-chip playback uses two
    /// separate emulator instances.
    /// </param>
    /// <param name="stopAfterOrderList">
    /// When set to <see langword="true"/>, PT3-based playback stops after the order list instead of looping to the
    /// restart position.
    /// </param>
    /// <returns>A playback session that owns the created emulator instances.</returns>
    public static RetroSoundPlaybackSession CreateFromFile(
        string filePath,
        int sampleRate,
        Func<IChipEmulator> createChipEmulator,
        bool stopAfterOrderList = true)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("The file path must not be null, empty, or whitespace.", nameof(filePath));
        }

        if (sampleRate <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sampleRate), "Sample rate must be greater than zero.");
        }

        ArgumentNullException.ThrowIfNull(createChipEmulator);

        var inputKind = TurboSoundInputDetector.DetectFromFile(filePath);
        return inputKind switch
        {
            TurboSoundInputKind.TsContainer => CreateTurboSoundContainerSession(filePath, sampleRate, createChipEmulator),
            TurboSoundInputKind.Pt3Module => CreatePt3Session(filePath, sampleRate, createChipEmulator, stopAfterOrderList),
            TurboSoundInputKind.Pt3TurboSoundModule => CreatePt3TurboSoundSession(filePath, sampleRate, createChipEmulator, stopAfterOrderList),
            _ => throw new NotSupportedException(
                "The input is not a supported RetroSound format. Expected a TS container, a PT3 module signature, or a PT3 TurboSound module."),
        };
    }

    /// <summary>
    /// Releases emulator resources owned by this playback session.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        for (var index = _ownedResources.Length - 1; index >= 0; index--)
        {
            _ownedResources[index].Dispose();
        }

        _disposed = true;
    }

    private static RetroSoundPlaybackSession CreateTurboSoundContainerSession(
        string filePath,
        int sampleRate,
        Func<IChipEmulator> createChipEmulator)
    {
        var container = new TsContainerParser().LoadFromFile(filePath);
        var chipAEmulator = createChipEmulator();
        var chipBEmulator = createChipEmulator();
        var player = new DualChipPlayer(
            new RegisterFramePlayer(container.FirstModule.Payload, sampleRate),
            new RegisterFramePlayer(container.SecondModule.Payload, sampleRate));
        var provider = new RetroSoundSampleProvider(player, chipAEmulator, chipBEmulator);

        return new RetroSoundPlaybackSession(
            provider,
            TurboSoundInputKind.TsContainer,
            Path.GetFileName(filePath),
            loopingController: null,
            CaptureOwnedResources(chipAEmulator, chipBEmulator));
    }

    private static RetroSoundPlaybackSession CreatePt3Session(
        string filePath,
        int sampleRate,
        Func<IChipEmulator> createChipEmulator,
        bool stopAfterOrderList)
    {
        var module = new Pt3ModuleLoader().LoadFromFile(filePath);
        var emulator = createChipEmulator();
        var player = new Pt3Player(module, sampleRate, stopAfterOrderList);
        var provider = new RetroSoundSampleProvider(player, emulator);

        return new RetroSoundPlaybackSession(
            provider,
            TurboSoundInputKind.Pt3Module,
            module.Metadata.Title,
            player,
            CaptureOwnedResources(emulator));
    }

    private static RetroSoundPlaybackSession CreatePt3TurboSoundSession(
        string filePath,
        int sampleRate,
        Func<IChipEmulator> createChipEmulator,
        bool stopAfterOrderList)
    {
        var module = new Pt3TurboSoundModuleLoader().LoadFromFile(filePath);
        var chipAEmulator = createChipEmulator();
        var chipBEmulator = createChipEmulator();
        var player = new DualChipPlayer(
            new Pt3Player(module.FirstChip, sampleRate, stopAfterOrderList),
            new Pt3Player(module.SecondChip, sampleRate, stopAfterOrderList));
        var provider = new RetroSoundSampleProvider(player, chipAEmulator, chipBEmulator);

        return new RetroSoundPlaybackSession(
            provider,
            TurboSoundInputKind.Pt3TurboSoundModule,
            module.Title ?? Path.GetFileName(filePath),
            player,
            CaptureOwnedResources(chipAEmulator, chipBEmulator));
    }

    private static IDisposable[] CaptureOwnedResources(params IChipEmulator[] emulators)
    {
        var resources = new List<IDisposable>(emulators.Length);

        foreach (var emulator in emulators)
        {
            if (emulator is IDisposable disposable)
            {
                resources.Add(disposable);
            }
        }

        return resources.ToArray();
    }
}
