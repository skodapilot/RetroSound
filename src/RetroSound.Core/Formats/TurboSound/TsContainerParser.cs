// SPDX-FileCopyrightText: 2026 Anton Shirokov
// SPDX-License-Identifier: MIT

using System.Buffers.Binary;
using RetroSound.Core.Models;
namespace RetroSound.Core.Formats.TurboSound;

/// <summary>
/// Parses the current size-prefixed TS container layout into two logical module payloads.
/// </summary>
public sealed class TsContainerParser : ITurboSoundContainerLoader
{
    private const int HeaderSize = 10;
    private const string Pt3FormatHint = "PT3";
    private static ReadOnlySpan<byte> ShortPt3Signature => "PT3"u8;
    private static ReadOnlySpan<byte> StandardPt3Signature => "ProTracker 3."u8;

    /// <summary>
    /// Loads a TS container from a byte array.
    /// </summary>
    /// <param name="data">The container bytes to parse.</param>
    /// <returns>The parsed container.</returns>
    public TurboSoundContainer Load(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        return ParseCore(data);
    }

    /// <summary>
    /// Loads a TS container from a readable stream.
    /// </summary>
    /// <param name="source">The readable stream containing container bytes.</param>
    /// <returns>The parsed container.</returns>
    public TurboSoundContainer Load(Stream source)
    {
        ArgumentNullException.ThrowIfNull(source);
        EnsureReadable(source);

        using var buffer = new MemoryStream();
        source.CopyTo(buffer);
        return ParseCore(buffer.ToArray());
    }

    /// <summary>
    /// Loads a TS container from a file path.
    /// </summary>
    /// <param name="filePath">The path to the TS container file.</param>
    /// <returns>The parsed container.</returns>
    public TurboSoundContainer LoadFromFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("The file path must not be null, empty, or whitespace.", nameof(filePath));
        }

        using var stream = File.OpenRead(filePath);
        return Load(stream);
    }

    /// <inheritdoc />
    public async ValueTask<TurboSoundContainer> LoadAsync(
        Stream source,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        EnsureReadable(source);

        await using var buffer = new MemoryStream();
        await source.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
        return ParseCore(buffer.ToArray());
    }

    private static TurboSoundContainer ParseCore(ReadOnlySpan<byte> data)
    {
        if (data.IsEmpty)
        {
            throw new TurboSoundContainerFormatException("The TS container is empty.");
        }

        if (data.Length < HeaderSize)
        {
            throw new TurboSoundContainerFormatException(
                $"The TS container header is truncated. Expected at least {HeaderSize} bytes but found {data.Length}.");
        }

        if (TurboSoundInputDetector.Detect(data) != TurboSoundInputKind.TsContainer)
        {
            throw new TurboSoundContainerFormatException("The TS container signature is invalid. Expected ASCII 'TS'.");
        }

        var firstPayloadLength = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(2, sizeof(uint)));
        var secondPayloadLength = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(6, sizeof(uint)));

        if (firstPayloadLength == 0)
        {
            throw new TurboSoundContainerFormatException("The first TS module payload is empty.");
        }

        if (secondPayloadLength == 0)
        {
            throw new TurboSoundContainerFormatException("The second TS module payload is empty.");
        }

        var expectedLength = checked((long)HeaderSize + firstPayloadLength + secondPayloadLength);
        if (data.Length < expectedLength)
        {
            throw new TurboSoundContainerFormatException(
                $"The TS container is truncated. Expected {expectedLength} bytes but found {data.Length}.");
        }

        if (data.Length > expectedLength)
        {
            throw new TurboSoundContainerFormatException(
                $"The TS container has {data.Length - expectedLength} unexpected trailing bytes.");
        }

        var firstPayload = data.Slice(HeaderSize, checked((int)firstPayloadLength)).ToArray();
        var secondPayload = data.Slice(HeaderSize + checked((int)firstPayloadLength), checked((int)secondPayloadLength)).ToArray();

        return new TurboSoundContainer(
            title: null,
            firstModule: new TurboSoundContainerEntry(0, DetectFormatHint(firstPayload), firstPayload),
            secondModule: new TurboSoundContainerEntry(1, DetectFormatHint(secondPayload), secondPayload));
    }

    private static string? DetectFormatHint(ReadOnlySpan<byte> payload)
    {
        if (payload.Length >= StandardPt3Signature.Length && payload[..StandardPt3Signature.Length].SequenceEqual(StandardPt3Signature))
        {
            return Pt3FormatHint;
        }

        if (payload.Length >= ShortPt3Signature.Length && payload[..ShortPt3Signature.Length].SequenceEqual(ShortPt3Signature))
        {
            return Pt3FormatHint;
        }

        return null;
    }

    private static void EnsureReadable(Stream source)
    {
        if (!source.CanRead)
        {
            throw new ArgumentException("The source stream must be readable.", nameof(source));
        }
    }
}
