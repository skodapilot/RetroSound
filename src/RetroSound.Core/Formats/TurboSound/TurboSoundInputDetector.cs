// SPDX-FileCopyrightText: 2026 Anton Shirokov
// SPDX-License-Identifier: MIT

using RetroSound.Core.Formats.Pt3;
namespace RetroSound.Core.Formats.TurboSound;

/// <summary>
/// Detects known TurboSound-related binary formats from their leading signature bytes.
/// </summary>
public static class TurboSoundInputDetector
{
    private static ReadOnlySpan<byte> TsSignature => "TS"u8;
    private static ReadOnlySpan<byte> ShortPt3Signature => "PT3"u8;
    private static ReadOnlySpan<byte> StandardPt3Signature => "ProTracker 3."u8;
    private static ReadOnlySpan<byte> VortexTrackerSignature => "Vortex Tracker II 1.0 module:"u8;

    /// <summary>
    /// Detects the input kind from the provided bytes.
    /// </summary>
    /// <param name="data">The leading bytes to inspect.</param>
    /// <returns>The detected input kind.</returns>
    public static TurboSoundInputKind Detect(ReadOnlySpan<byte> data)
    {
        if (data.Length >= TsSignature.Length && data[..TsSignature.Length].SequenceEqual(TsSignature))
        {
            return TurboSoundInputKind.TsContainer;
        }

        if (StartsWithPt3Header(data))
        {
            if (Pt3TurboSoundModuleLoader.HasEmbeddedTurboSoundModule(data))
            {
                return TurboSoundInputKind.Pt3TurboSoundModule;
            }

            return TurboSoundInputKind.Pt3Module;
        }

        if (data.Length >= ShortPt3Signature.Length && data[..ShortPt3Signature.Length].SequenceEqual(ShortPt3Signature))
        {
            return TurboSoundInputKind.Pt3Module;
        }

        return TurboSoundInputKind.Unknown;
    }

    /// <summary>
    /// Detects the input kind by reading the leading bytes from a readable stream.
    /// </summary>
    /// <param name="source">The stream to inspect. Its original position is restored when seeking is supported.</param>
    /// <returns>The detected input kind.</returns>
    public static TurboSoundInputKind Detect(Stream source)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (!source.CanRead)
        {
            throw new ArgumentException("The source stream must be readable.", nameof(source));
        }

        var originalPosition = source.CanSeek ? source.Position : 0;
        using var buffer = new MemoryStream();
        source.CopyTo(buffer);

        if (source.CanSeek)
        {
            source.Position = originalPosition;
        }

        return Detect(buffer.ToArray());
    }

    /// <summary>
    /// Detects the input kind by reading the leading bytes from a file.
    /// </summary>
    /// <param name="filePath">The path to the input file.</param>
    /// <returns>The detected input kind.</returns>
    public static TurboSoundInputKind DetectFromFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("The file path must not be null, empty, or whitespace.", nameof(filePath));
        }

        using var stream = File.OpenRead(filePath);
        return Detect(stream);
    }

    private static bool StartsWithPt3Header(ReadOnlySpan<byte> data)
    {
        return data.Length >= StandardPt3Signature.Length && data[..StandardPt3Signature.Length].SequenceEqual(StandardPt3Signature)
            || data.Length >= VortexTrackerSignature.Length && data[..VortexTrackerSignature.Length].SequenceEqual(VortexTrackerSignature);
    }
}