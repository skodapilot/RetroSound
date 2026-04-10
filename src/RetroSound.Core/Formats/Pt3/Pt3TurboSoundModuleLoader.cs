// SPDX-FileCopyrightText: 2026 Anton Shirokov
// SPDX-License-Identifier: MIT

namespace RetroSound.Core.Formats.Pt3;

/// <summary>
/// Loads PT3 TurboSound files that embed one PT3 module per chip in a single binary stream.
/// </summary>
public sealed class Pt3TurboSoundModuleLoader
{
    private static ReadOnlySpan<byte> StandardPt3Signature => "ProTracker 3."u8;
    private static ReadOnlySpan<byte> VortexTrackerSignature => "Vortex Tracker II 1.0 module:"u8;

    /// <summary>
    /// Loads a PT3 TurboSound module from a byte array.
    /// </summary>
    /// <param name="data">The PT3 TurboSound bytes to parse.</param>
    /// <returns>The parsed dual-chip PT3 module.</returns>
    public Pt3TurboSoundModule Load(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        return LoadWithDiagnostics(data).Module;
    }

    /// <summary>
    /// Analyzes PT3 bytes and returns embedded-module diagnostics even when the file is not a dual-chip TurboSound module.
    /// </summary>
    /// <param name="data">The PT3 bytes to analyze.</param>
    /// <returns>The discovered PT3 candidate diagnostics.</returns>
    public Pt3TurboSoundLoadDiagnostics Analyze(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        return AnalyzeCore(data).Diagnostics;
    }

    /// <summary>
    /// Loads a PT3 TurboSound module from a readable stream.
    /// </summary>
    /// <param name="source">The readable stream containing PT3 TurboSound bytes.</param>
    /// <returns>The parsed dual-chip PT3 module.</returns>
    public Pt3TurboSoundModule Load(Stream source)
    {
        ArgumentNullException.ThrowIfNull(source);
        EnsureReadable(source);

        using var buffer = new MemoryStream();
        source.CopyTo(buffer);
        return LoadWithDiagnostics(buffer.ToArray()).Module;
    }

    /// <summary>
    /// Analyzes PT3 bytes from a readable stream and returns embedded-module diagnostics even when the file is not a dual-chip TurboSound module.
    /// </summary>
    /// <param name="source">The readable stream containing PT3 bytes.</param>
    /// <returns>The discovered PT3 candidate diagnostics.</returns>
    public Pt3TurboSoundLoadDiagnostics Analyze(Stream source)
    {
        ArgumentNullException.ThrowIfNull(source);
        EnsureReadable(source);

        using var buffer = new MemoryStream();
        source.CopyTo(buffer);
        return Analyze(buffer.ToArray());
    }

    /// <summary>
    /// Loads a PT3 TurboSound module from a file path.
    /// </summary>
    /// <param name="filePath">The path to the PT3 TurboSound file.</param>
    /// <returns>The parsed dual-chip PT3 module.</returns>
    public Pt3TurboSoundModule LoadFromFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("The file path must not be null, empty, or whitespace.", nameof(filePath));
        }

        using var stream = File.OpenRead(filePath);
        return Load(stream);
    }

    /// <summary>
    /// Analyzes a PT3 file and returns embedded-module diagnostics even when the file is not a dual-chip TurboSound module.
    /// </summary>
    /// <param name="filePath">The path to the PT3 file.</param>
    /// <returns>The discovered PT3 candidate diagnostics.</returns>
    public Pt3TurboSoundLoadDiagnostics AnalyzeFromFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("The file path must not be null, empty, or whitespace.", nameof(filePath));
        }

        using var stream = File.OpenRead(filePath);
        return Analyze(stream);
    }

    /// <summary>
    /// Loads a PT3 TurboSound module from a stream asynchronously.
    /// </summary>
    /// <param name="source">The readable stream containing PT3 TurboSound bytes.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The parsed dual-chip PT3 module.</returns>
    public async ValueTask<Pt3TurboSoundModule> LoadAsync(
        Stream source,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        EnsureReadable(source);

        await using var buffer = new MemoryStream();
        await source.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
        return (await LoadWithDiagnosticsAsync(buffer.ToArray(), cancellationToken).ConfigureAwait(false)).Module;
    }

    /// <summary>
    /// Loads a PT3 TurboSound module from a byte array and returns detailed diagnostics.
    /// </summary>
    /// <param name="data">The PT3 TurboSound bytes to parse.</param>
    /// <returns>The parsed dual-chip PT3 module and its diagnostics.</returns>
    public Pt3TurboSoundLoadResult LoadWithDiagnostics(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        return ParseCore(data);
    }

    /// <summary>
    /// Loads a PT3 TurboSound module from a file path and returns detailed diagnostics.
    /// </summary>
    /// <param name="filePath">The path to the PT3 TurboSound file.</param>
    /// <returns>The parsed dual-chip PT3 module and its diagnostics.</returns>
    public Pt3TurboSoundLoadResult LoadWithDiagnosticsFromFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("The file path must not be null, empty, or whitespace.", nameof(filePath));
        }

        using var stream = File.OpenRead(filePath);
        return LoadWithDiagnostics(stream);
    }

    /// <summary>
    /// Loads a PT3 TurboSound module from a readable stream and returns detailed diagnostics.
    /// </summary>
    /// <param name="source">The readable stream containing PT3 TurboSound bytes.</param>
    /// <returns>The parsed dual-chip PT3 module and its diagnostics.</returns>
    public Pt3TurboSoundLoadResult LoadWithDiagnostics(Stream source)
    {
        ArgumentNullException.ThrowIfNull(source);
        EnsureReadable(source);

        using var buffer = new MemoryStream();
        source.CopyTo(buffer);
        return ParseCore(buffer.ToArray());
    }

    /// <summary>
    /// Loads a PT3 TurboSound module from bytes asynchronously and returns detailed diagnostics.
    /// </summary>
    /// <param name="data">The PT3 TurboSound bytes to parse.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The parsed dual-chip PT3 module and its diagnostics.</returns>
    public ValueTask<Pt3TurboSoundLoadResult> LoadWithDiagnosticsAsync(
        byte[] data,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(data);
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(ParseCore(data));
    }

    /// <summary>
    /// Determines whether the provided PT3 bytes contain a second embedded PT3 module.
    /// </summary>
    /// <param name="data">The bytes to inspect.</param>
    /// <returns><see langword="true"/> when a second PT3 module can be parsed; otherwise, <see langword="false"/>.</returns>
    public static bool HasEmbeddedTurboSoundModule(ReadOnlySpan<byte> data)
    {
        if (!StartsWithPt3Signature(data))
        {
            return false;
        }

        var loader = new Pt3ModuleLoader();
        foreach (var offset in FindEmbeddedModuleOffsets(data))
        {
            try
            {
                loader.Load(data[offset..].ToArray());
                return true;
            }
            catch (Pt3FormatException)
            {
            }
        }

        return false;
    }

    private static Pt3TurboSoundLoadResult ParseCore(ReadOnlySpan<byte> data)
    {
        var analysis = AnalyzeCore(data);
        if (analysis.SecondChip is null)
        {
            throw new Pt3FormatException("The PT3 TurboSound module does not contain a valid second embedded PT3 module.");
        }

        var module = new Pt3TurboSoundModule(
            analysis.FirstChip,
            analysis.SecondChip,
            analysis.FirstChip.Metadata.Title,
            analysis.Diagnostics);
        return new Pt3TurboSoundLoadResult(module, analysis.Diagnostics);
    }

    private static Pt3TurboSoundAnalysis AnalyzeCore(ReadOnlySpan<byte> data)
    {
        if (!StartsWithPt3Signature(data))
        {
            throw new Pt3FormatException(
                "The PT3 TurboSound module signature is invalid. Expected the file to start with a PT3 module header.");
        }

        var pt3Loader = new Pt3ModuleLoader();
        var candidates = new List<Pt3TurboSoundModuleCandidateInfo>();
        var firstChip = pt3Loader.Load(data.ToArray());
        candidates.Add(CreateSuccessfulCandidate(offset: 0, DetectHeaderKind(data), usage: "Used as chip A", firstChip));

        Pt3Module? secondChip = null;
        foreach (var offset in FindEmbeddedModuleOffsets(data))
        {
            var headerKind = DetectHeaderKind(data[offset..]);
            try
            {
                var parsedModule = pt3Loader.Load(data[offset..].ToArray());
                if (secondChip is null)
                {
                    secondChip = parsedModule;
                    candidates.Add(CreateSuccessfulCandidate(offset, headerKind, "Used as chip B", parsedModule));
                }
                else
                {
                    candidates.Add(CreateSuccessfulCandidate(offset, headerKind, "Skipped as extra valid embedded module", parsedModule));
                }
            }
            catch (Pt3FormatException exception)
            {
                candidates.Add(CreateFailedCandidate(offset, headerKind, "Skipped as invalid embedded module", exception.Message));
            }
        }

        return new Pt3TurboSoundAnalysis(
            firstChip,
            secondChip,
            new Pt3TurboSoundLoadDiagnostics(data.Length, candidates));
    }

    private static List<int> FindEmbeddedModuleOffsets(ReadOnlySpan<byte> data)
    {
        var offsets = new List<int>();
        var minimumOffset = StandardPt3Signature.Length;
        for (var offset = minimumOffset; offset < data.Length; offset++)
        {
            if (MatchesSignature(data, offset, StandardPt3Signature) || MatchesSignature(data, offset, VortexTrackerSignature))
            {
                offsets.Add(offset);
            }
        }

        return offsets;
    }

    private static bool StartsWithPt3Signature(ReadOnlySpan<byte> data)
    {
        return data.Length >= StandardPt3Signature.Length && data[..StandardPt3Signature.Length].SequenceEqual(StandardPt3Signature)
            || data.Length >= VortexTrackerSignature.Length && data[..VortexTrackerSignature.Length].SequenceEqual(VortexTrackerSignature);
    }

    private static bool MatchesSignature(ReadOnlySpan<byte> data, int offset, ReadOnlySpan<byte> signature)
    {
        return offset >= 0
            && offset + signature.Length <= data.Length
            && data.Slice(offset, signature.Length).SequenceEqual(signature);
    }

    private static void EnsureReadable(Stream source)
    {
        if (!source.CanRead)
        {
            throw new ArgumentException("The source stream must be readable.", nameof(source));
        }
    }

    private static string DetectHeaderKind(ReadOnlySpan<byte> data)
    {
        if (MatchesSignature(data, 0, StandardPt3Signature))
        {
            return "ProTracker 3.x";
        }

        if (MatchesSignature(data, 0, VortexTrackerSignature))
        {
            return "Vortex Tracker II 1.0";
        }

        return "Unknown";
    }

    private static Pt3TurboSoundModuleCandidateInfo CreateSuccessfulCandidate(
        int offset,
        string headerKind,
        string usage,
        Pt3Module module)
    {
        return new Pt3TurboSoundModuleCandidateInfo(
            offset,
            headerKind,
            parsedSuccessfully: true,
            usage,
            module.Metadata,
            module.FrequencyTable,
            module.Tempo,
            failureReason: null);
    }

    private static Pt3TurboSoundModuleCandidateInfo CreateFailedCandidate(
        int offset,
        string headerKind,
        string usage,
        string failureReason)
    {
        return new Pt3TurboSoundModuleCandidateInfo(
            offset,
            headerKind,
            parsedSuccessfully: false,
            usage,
            metadata: null,
            frequencyTable: null,
            tempo: null,
            failureReason);
    }

    private sealed record Pt3TurboSoundAnalysis(
        Pt3Module FirstChip,
        Pt3Module? SecondChip,
        Pt3TurboSoundLoadDiagnostics Diagnostics);
}