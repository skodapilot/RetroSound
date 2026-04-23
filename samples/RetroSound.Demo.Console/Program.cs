// SPDX-FileCopyrightText: 2026 Anton Shirokov
// SPDX-License-Identifier: MIT

using RetroSound.Ayumi;
using RetroSound.Core.Formats.Pt3;
using RetroSound.Core.Formats.TurboSound;
using RetroSound.Core.Models;
using RetroSound.Core.Playback;
using RetroSound.Core.Rendering;
using RetroSound.NAudio;
using RetroSound.NAudio.WaveOut;

const int defaultSampleRate = 48_000;
const float volumeStep = 0.1f;

if (args.Length == 0)
{
    Console.WriteLine("Usage: RetroSound.Demo.Console <path-to-input> [sample-rate]");
    Console.WriteLine("Supported inputs: TS containers with raw AY frames, standalone PT3 modules, and PT3 TurboSound modules.");
    return;
}

var filePath = Path.GetFullPath(args[0]);
var sampleRate = ResolveSampleRate(args);
await PlaySupportedInputAsync(filePath, sampleRate);

static int ResolveSampleRate(string[] args)
{
    if (args.Length < 2)
    {
        return defaultSampleRate;
    }

    if (int.TryParse(args[1], out var sampleRate) && sampleRate > 0)
    {
        return sampleRate;
    }

    throw new ArgumentException("The optional sample rate must be a positive integer.", nameof(args));
}

static async Task PlaySupportedInputAsync(string filePath, int sampleRate)
{
    var inputKind = TurboSoundInputDetector.DetectFromFile(filePath);
    var diagnostics = inputKind is TurboSoundInputKind.Pt3Module or TurboSoundInputKind.Pt3TurboSoundModule
        ? new Pt3TurboSoundModuleLoader().AnalyzeFromFile(filePath)
        : null;

    using var session = RetroSoundPlaybackSession.CreateFromFile(
        filePath,
        sampleRate,
        CreateStereoEmulator,
        stopAfterOrderList: true);
    using var playback = CreatePlaybackService(session.SampleProvider);

    var displayTitle = string.IsNullOrWhiteSpace(session.Title)
        ? Path.GetFileName(filePath)
        : session.Title;

    Console.WriteLine($"Playing {DescribeInputKind(session.InputKind)} '{displayTitle}' from '{filePath}' at {sampleRate} Hz.");

    if (diagnostics is not null)
    {
        PrintPt3TurboSoundDiagnostics(diagnostics);
    }

    Console.WriteLine(DescribePlaybackFlow(session.InputKind));
    PrintPlaybackControls(session.LoopingController);

    await RunPlaybackAsync(session.SampleProvider, playback, session.LoopingController);

    Console.WriteLine("Playback completed.");
}

static string DescribeInputKind(TurboSoundInputKind inputKind)
{
    return inputKind switch
    {
        TurboSoundInputKind.TsContainer => "TS container",
        TurboSoundInputKind.Pt3Module => "PT3 module",
        TurboSoundInputKind.Pt3TurboSoundModule => "PT3 TurboSound module",
        _ => "RetroSound input",
    };
}

static string DescribePlaybackFlow(TurboSoundInputKind inputKind)
{
    return inputKind switch
    {
        TurboSoundInputKind.TsContainer => "Playback flow: TS parser -> dual tick player -> AY/YM emulators -> stereo mix -> NAudio.",
        TurboSoundInputKind.Pt3Module => "Playback flow: PT3 parser -> PT3 tick player -> AY/YM emulator -> NAudio.",
        TurboSoundInputKind.Pt3TurboSoundModule => "Playback flow: PT3 TurboSound loader -> two PT3 tick players -> AY/YM emulators -> stereo mix -> NAudio.",
        _ => "Playback flow: parser -> playback graph -> AY/YM emulation -> NAudio.",
    };
}

static async Task RunPlaybackAsync(
    RetroSoundSampleProvider provider,
    WaveOutPlayer playback,
    ILoopingPlaybackController? loopingController)
{
    using var stopRequested = new CancellationTokenSource();
    ConsoleCancelEventHandler? cancelHandler = null;

    cancelHandler = (_, eventArgs) =>
    {
        eventArgs.Cancel = true;
        stopRequested.Cancel();
    };

    Console.CancelKeyPress += cancelHandler;

    try
    {
        playback.Start();

        var inputTask = MonitorPlaybackInputAsync(playback, loopingController, stopRequested.Token);
        var endOfStreamTask = provider.WaitForEndOfStreamAsync(stopRequested.Token);

        try
        {
            await endOfStreamTask;
        }
        catch (OperationCanceledException) when (stopRequested.IsCancellationRequested)
        {
        }

        playback.Stop();
        stopRequested.Cancel();
        await Task.WhenAny(inputTask, Task.Delay(50));
        await playback.WaitForPlaybackStoppedAsync();
    }
    finally
    {
        Console.CancelKeyPress -= cancelHandler;
    }
}

static async Task MonitorPlaybackInputAsync(
    WaveOutPlayer playback,
    ILoopingPlaybackController? loopingController,
    CancellationToken cancellationToken)
{
    while (!cancellationToken.IsCancellationRequested)
    {
        if (!CanReadConsoleInput())
        {
            return;
        }

        if (!Console.KeyAvailable)
        {
            await Task.Delay(50, cancellationToken);
            continue;
        }

        var key = Console.ReadKey(intercept: true);
        if (key.Key is ConsoleKey.P or ConsoleKey.Spacebar)
        {
            if (playback.IsPaused)
            {
                playback.Resume();
                Console.WriteLine("Playback resumed.");
            }
            else
            {
                playback.Pause();
                Console.WriteLine("Playback paused.");
            }
        }
        else if (key.Key == ConsoleKey.L && loopingController?.SupportsLooping == true)
        {
            loopingController.IsLoopingEnabled = !loopingController.IsLoopingEnabled;
            Console.WriteLine($"Loop playback {(loopingController.IsLoopingEnabled ? "enabled" : "disabled")}.");
        }
        else if (IsVolumeDownKey(key))
        {
            playback.Volume = Math.Max(0.0f, playback.Volume - volumeStep);
            Console.WriteLine($"Playback volume: {playback.Volume:P0}.");
        }
        else if (IsVolumeUpKey(key))
        {
            playback.Volume = Math.Min(1.0f, playback.Volume + volumeStep);
            Console.WriteLine($"Playback volume: {playback.Volume:P0}.");
        }
    }
}

static void PrintPlaybackControls(ILoopingPlaybackController? loopingController)
{
    Console.WriteLine("Press P or Space to pause/resume.");
    Console.WriteLine("Press - or + to decrease or increase playback volume.");

    if (loopingController?.SupportsLooping == true)
    {
        Console.WriteLine($"Press L to toggle loop playback (currently {(loopingController.IsLoopingEnabled ? "on" : "off")}).");
    }

    Console.WriteLine("Press Ctrl+C to stop the demo early.");
}

static bool CanReadConsoleInput()
{
    try
    {
        _ = Console.KeyAvailable;
        return true;
    }
    catch (InvalidOperationException)
    {
        return false;
    }
}

static bool IsVolumeDownKey(ConsoleKeyInfo key)
{
    return key.Key is ConsoleKey.Subtract or ConsoleKey.OemMinus || key.KeyChar == '-';
}

static bool IsVolumeUpKey(ConsoleKeyInfo key)
{
    return key.Key is ConsoleKey.Add or ConsoleKey.OemPlus || key.KeyChar == '+';
}

static void PrintPt3TurboSoundDiagnostics(Pt3TurboSoundLoadDiagnostics diagnostics)
{
    Console.WriteLine(
        $"Load diagnostics: file length {diagnostics.FileLength} bytes, discovered {diagnostics.DiscoveredModuleCount} module candidate(s), parsed {diagnostics.ParsedModuleCount}, used {diagnostics.UsedModuleCount}, skipped {diagnostics.SkippedModuleCount}.");

    foreach (var candidate in diagnostics.Candidates)
    {
        var metadataSuffix = candidate.Metadata is null
            ? string.Empty
            : $" | title='{candidate.Metadata.Title}' author='{candidate.Metadata.Author}' version={candidate.Metadata.Version} table={candidate.FrequencyTable} tempo={candidate.Tempo}";
        var failureSuffix = string.IsNullOrWhiteSpace(candidate.FailureReason)
            ? string.Empty
            : $" | reason: {candidate.FailureReason}";

        Console.WriteLine(
            $"  offset {candidate.Offset,5} | {candidate.HeaderKind,-25} | {candidate.Usage,-38} | parsed={(candidate.ParsedSuccessfully ? "yes" : "no")}{metadataSuffix}{failureSuffix}");
    }
}

static AyYmChipEmulator CreateStereoEmulator()
{
    return new AyYmChipEmulator(
        new AyumiPcmRenderer(),
        new AyYmChipConfiguration(outputChannelCount: 2));
}

static WaveOutPlayer CreatePlaybackService(RetroSoundSampleProvider provider)
{
    return new WaveOutPlayer(
        provider,
        new WaveOutOptions
        {
            DesiredLatencyMilliseconds = 120,
            NumberOfBuffers = 2,
        });
}
