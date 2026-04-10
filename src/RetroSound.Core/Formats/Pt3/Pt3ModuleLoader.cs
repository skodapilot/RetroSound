// SPDX-FileCopyrightText: 2026 Anton Shirokov
// SPDX-License-Identifier: MIT

using System.Buffers.Binary;
using System.Text;
namespace RetroSound.Core.Formats.Pt3;

/// <summary>
/// Loads standard PT3 tracker modules into a structured internal representation.
/// </summary>
/// <remarks>
/// The parser behavior is aligned with the PT3 structures used by the public `pt3player` reference project:
/// https://github.com/Volutar/pt3player.
/// </remarks>
public sealed class Pt3ModuleLoader
{
    private const int HeaderSize = 201;
    private const int OrderListOffset = 201;
    private const int PatternPointerEntrySize = 6;
    private const int SampleCount = 32;
    private const int OrnamentCount = 16;
    private static ReadOnlySpan<byte> StandardSignature => "ProTracker 3."u8;
    private static ReadOnlySpan<byte> VortexTrackerSignature => "Vortex Tracker II 1.0 module:"u8;

    /// <summary>
    /// Loads a PT3 module from a byte array.
    /// </summary>
    /// <param name="data">The PT3 bytes to parse.</param>
    /// <returns>The parsed PT3 module.</returns>
    public Pt3Module Load(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        return ParseCore(data);
    }

    /// <summary>
    /// Loads a PT3 module from a readable stream.
    /// </summary>
    /// <param name="source">The readable stream containing PT3 bytes.</param>
    /// <returns>The parsed PT3 module.</returns>
    public Pt3Module LoadFromStream(Stream source)
    {
        ArgumentNullException.ThrowIfNull(source);
        EnsureReadable(source);

        using var buffer = new MemoryStream();
        source.CopyTo(buffer);
        return ParseCore(buffer.ToArray());
    }

    /// <summary>
    /// Loads a PT3 module from a file path.
    /// </summary>
    /// <param name="filePath">The path to the PT3 file.</param>
    /// <returns>The parsed PT3 module.</returns>
    public Pt3Module LoadFromFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("The file path must not be null, empty, or whitespace.", nameof(filePath));
        }

        using var stream = File.OpenRead(filePath);
        return LoadFromStream(stream);
    }

    /// <summary>
    /// Loads a PT3 module from a stream asynchronously.
    /// </summary>
    /// <param name="source">The readable stream containing PT3 bytes.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The parsed PT3 module.</returns>
    public async ValueTask<Pt3Module> LoadAsync(
        Stream source,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        EnsureReadable(source);

        await using var buffer = new MemoryStream();
        await source.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
        return ParseCore(buffer.ToArray());
    }

    private static Pt3Module ParseCore(ReadOnlySpan<byte> data)
    {
        if (data.IsEmpty)
        {
            throw new Pt3FormatException("The PT3 module is empty.");
        }

        if (data.Length < HeaderSize)
        {
            throw new Pt3FormatException(
                $"The PT3 module header is truncated. Expected at least {HeaderSize} bytes but found {data.Length}.");
        }

        var headerKind = DetectHeaderKind(data);
        if (headerKind == Pt3HeaderKind.Unknown)
        {
            throw new Pt3FormatException(
                "The PT3 module signature is invalid. Expected ASCII 'ProTracker 3.' or 'Vortex Tracker II 1.0 module:'.");
        }

        var frequencyTable = ReadFrequencyTable(data[99]);
        var tempo = data[100];
        if (tempo == 0)
        {
            throw new Pt3FormatException("The PT3 tempo value is invalid. Expected a value greater than zero.");
        }

        var restartPositionIndex = data[102];
        var patternTableOffset = ReadOffset(data, 103, "pattern pointer table");
        if (patternTableOffset < OrderListOffset)
        {
            throw new Pt3FormatException(
                $"The PT3 pattern pointer table offset {patternTableOffset} overlaps the fixed-size header area.");
        }

        if (patternTableOffset >= data.Length)
        {
            throw new Pt3FormatException(
                $"The PT3 pattern pointer table offset {patternTableOffset} is outside the module bounds ({data.Length} bytes).");
        }

        var metadata = ExtractMetadata(data, headerKind);

        var order = ParseOrder(data, patternTableOffset);
        if (restartPositionIndex >= order.Count)
        {
            throw new Pt3FormatException(
                $"The PT3 restart position {restartPositionIndex} is outside the order list length {order.Count}.");
        }

        var highestPatternIndex = order.Max();
        var requiredPatternTableBytes = checked((highestPatternIndex + 1) * PatternPointerEntrySize);
        if (patternTableOffset + requiredPatternTableBytes > data.Length)
        {
            throw new Pt3FormatException(
                $"The PT3 pattern pointer table is truncated. Expected {requiredPatternTableBytes} bytes for patterns 0..{highestPatternIndex}.");
        }

        var sampleOffsets = ReadOptionalOffsets(data, 105, SampleCount);
        var ornamentOffsets = ReadOptionalOffsets(data, 169, OrnamentCount);
        var patterns = ParsePatterns(data, patternTableOffset, highestPatternIndex, sampleOffsets, ornamentOffsets);
        var samples = ParseSamples(data, sampleOffsets);
        var ornaments = ParseOrnaments(data, ornamentOffsets);

        return new Pt3Module(
            metadata,
            frequencyTable,
            tempo,
            restartPositionIndex,
            order,
            patterns,
            samples,
            ornaments);
    }

    private static List<int> ParseOrder(ReadOnlySpan<byte> data, int patternTableOffset)
    {
        var order = new List<int>();
        for (var offset = OrderListOffset; offset < patternTableOffset; offset++)
        {
            var entry = data[offset];
            if (entry == 0xFF)
            {
                break;
            }

            if (entry % 3 != 0)
            {
                throw new Pt3FormatException(
                    $"The PT3 order entry at offset {offset} has value {entry}, which is not divisible by 3.");
            }

            order.Add(entry / 3);
        }

        if (order.Count == 0)
        {
            throw new Pt3FormatException("The PT3 order list is empty or missing its first pattern entry.");
        }

        if (OrderListOffset + order.Count >= patternTableOffset || data[OrderListOffset + order.Count] != 0xFF)
        {
            throw new Pt3FormatException("The PT3 order list is not terminated by 0xFF before the pattern table.");
        }

        return order;
    }

    private static List<Pt3Pattern> ParsePatterns(
        ReadOnlySpan<byte> data,
        int patternTableOffset,
        int highestPatternIndex,
        IReadOnlyList<int> sampleOffsets,
        IReadOnlyList<int> ornamentOffsets)
    {
        var patterns = new List<Pt3Pattern>(highestPatternIndex + 1);
        var channelOffsets = new List<int>(checked((highestPatternIndex + 1) * PatternPointerEntrySize / 2));

        for (var patternIndex = 0; patternIndex <= highestPatternIndex; patternIndex++)
        {
            var entryOffset = patternTableOffset + (patternIndex * PatternPointerEntrySize);
            var channelAOffset = ReadOffset(data, entryOffset + 0, $"pattern {patternIndex} channel A");
            var channelBOffset = ReadOffset(data, entryOffset + 2, $"pattern {patternIndex} channel B");
            var channelCOffset = ReadOffset(data, entryOffset + 4, $"pattern {patternIndex} channel C");
            channelOffsets.Add(channelAOffset);
            channelOffsets.Add(channelBOffset);
            channelOffsets.Add(channelCOffset);

            patterns.Add(new Pt3Pattern(
                patternIndex,
                new Pt3ChannelPattern("A", channelAOffset, ReadOnlyMemory<byte>.Empty),
                new Pt3ChannelPattern("B", channelBOffset, ReadOnlyMemory<byte>.Empty),
                new Pt3ChannelPattern("C", channelCOffset, ReadOnlyMemory<byte>.Empty)));
        }

        var boundarySet = new HashSet<int>(channelOffsets.Count + sampleOffsets.Count + ornamentOffsets.Count);
        foreach (var o in channelOffsets)
            if (o > 0) boundarySet.Add(o);
        foreach (var o in sampleOffsets)
            if (o > 0) boundarySet.Add(o);
        foreach (var o in ornamentOffsets)
            if (o > 0) boundarySet.Add(o);
        var boundaries = boundarySet.ToArray();
        Array.Sort(boundaries);

        for (var patternIndex = 0; patternIndex < patterns.Count; patternIndex++)
        {
            var pattern = patterns[patternIndex];
            patterns[patternIndex] = new Pt3Pattern(
                pattern.Index,
                ReadChannelPattern(data, pattern.ChannelA.Name, pattern.ChannelA.DataOffset, boundaries),
                ReadChannelPattern(data, pattern.ChannelB.Name, pattern.ChannelB.DataOffset, boundaries),
                ReadChannelPattern(data, pattern.ChannelC.Name, pattern.ChannelC.DataOffset, boundaries));
        }

        return patterns;
    }

    private static Pt3ChannelPattern ReadChannelPattern(
        ReadOnlySpan<byte> data,
        string name,
        int offset,
        ReadOnlySpan<int> boundaries)
    {
        if (offset <= 0 || offset >= data.Length)
        {
            throw new Pt3FormatException(
                $"The PT3 channel {name} pattern stream offset {offset} is outside the module bounds ({data.Length} bytes).");
        }

        var boundaryIndex = boundaries.BinarySearch(offset);
        if (boundaryIndex < 0)
        {
            throw new Pt3FormatException(
                $"The PT3 channel {name} pattern stream offset {offset} is not present in the known PT3 data boundaries.");
        }

        var nextBoundary = boundaryIndex + 1 < boundaries.Length ? boundaries[boundaryIndex + 1] : data.Length;
        if (nextBoundary <= offset)
        {
            throw new Pt3FormatException(
                $"The PT3 channel {name} pattern stream at offset {offset} does not have any bytes before the next data boundary.");
        }

        return new Pt3ChannelPattern(name, offset, data.Slice(offset, nextBoundary - offset).ToArray());
    }

    private static List<Pt3Sample> ParseSamples(ReadOnlySpan<byte> data, IReadOnlyList<int> offsets)
    {
        var samples = new List<Pt3Sample>();
        for (var index = 0; index < SampleCount; index++)
        {
            var offset = offsets[index];
            if (offset == 0)
            {
                continue;
            }

            if (offset + 2 > data.Length)
            {
                throw new Pt3FormatException(
                    $"The PT3 sample {index} header at offset {offset} is truncated.");
            }

            var loopPosition = data[offset];
            var length = data[offset + 1];
            if (length == 0)
            {
                throw new Pt3FormatException($"The PT3 sample {index} at offset {offset} has zero length.");
            }

            if (loopPosition >= length)
            {
                throw new Pt3FormatException(
                    $"The PT3 sample {index} loop position {loopPosition} is outside its {length} step(s).");
            }

            var stepsByteLength = checked(length * 4);
            var dataStart = offset + 2;
            var dataEnd = dataStart + stepsByteLength;
            if (dataEnd > data.Length)
            {
                throw new Pt3FormatException(
                    $"The PT3 sample {index} data is truncated. Expected {stepsByteLength} bytes of step data.");
            }

            var steps = new List<Pt3SampleStep>(length);
            for (var stepIndex = 0; stepIndex < length; stepIndex++)
            {
                var stepOffset = dataStart + (stepIndex * 4);
                steps.Add(new Pt3SampleStep(
                    data[stepOffset],
                    data[stepOffset + 1],
                    BinaryPrimitives.ReadInt16LittleEndian(data.Slice(stepOffset + 2, 2))));
            }

            samples.Add(new Pt3Sample(index, offset, loopPosition, steps));
        }

        return samples;
    }

    private static List<Pt3Ornament> ParseOrnaments(ReadOnlySpan<byte> data, IReadOnlyList<int> offsets)
    {
        var ornaments = new List<Pt3Ornament>();
        for (var index = 0; index < OrnamentCount; index++)
        {
            var offset = offsets[index];
            if (offset == 0)
            {
                continue;
            }

            if (offset + 2 > data.Length)
            {
                throw new Pt3FormatException(
                    $"The PT3 ornament {index} header at offset {offset} is truncated.");
            }

            var loopPosition = data[offset];
            var length = data[offset + 1];
            if (length == 0)
            {
                throw new Pt3FormatException($"The PT3 ornament {index} at offset {offset} has zero length.");
            }

            if (loopPosition >= length)
            {
                throw new Pt3FormatException(
                    $"The PT3 ornament {index} loop position {loopPosition} is outside its {length} step(s).");
            }

            var dataStart = offset + 2;
            var dataEnd = dataStart + length;
            if (dataEnd > data.Length)
            {
                throw new Pt3FormatException(
                    $"The PT3 ornament {index} data is truncated. Expected {length} byte(s) of ornament data.");
            }

            var toneOffsets = new sbyte[length];
            for (var valueIndex = 0; valueIndex < length; valueIndex++)
            {
                toneOffsets[valueIndex] = unchecked((sbyte)data[dataStart + valueIndex]);
            }

            ornaments.Add(new Pt3Ornament(index, offset, loopPosition, toneOffsets));
        }

        return ornaments;
    }

    private static Pt3FrequencyTableKind ReadFrequencyTable(byte value)
    {
        return value switch
        {
            0 => Pt3FrequencyTableKind.ProTracker,
            1 => Pt3FrequencyTableKind.SoundTracker,
            2 => Pt3FrequencyTableKind.AsmOrPsc,
            3 => Pt3FrequencyTableKind.RealSound,
            _ => throw new Pt3FormatException($"The PT3 frequency table value {value} is unsupported."),
        };
    }

    private static int ReadOffset(ReadOnlySpan<byte> data, int offset, string description)
    {
        if (offset < 0 || offset + 2 > data.Length)
        {
            throw new Pt3FormatException($"The PT3 {description} pointer at offset {offset} is truncated.");
        }

        return BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(offset, 2));
    }

    private static int ReadOptionalOffset(ReadOnlySpan<byte> data, int offset)
    {
        if (offset < 0 || offset + 2 > data.Length)
        {
            throw new Pt3FormatException($"The PT3 pointer at offset {offset} is truncated.");
        }

        return BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(offset, 2));
    }

    private static int[] ReadOptionalOffsets(ReadOnlySpan<byte> data, int startOffset, int count)
    {
        var offsets = new int[count];
        for (var index = 0; index < count; index++)
        {
            offsets[index] = ReadOptionalOffset(data, startOffset + (index * 2));
        }

        return offsets;
    }

    private static string ExtractAsciiTrimmed(ReadOnlySpan<byte> data)
    {
        return Encoding.ASCII.GetString(data).TrimEnd('\0', ' ');
    }

    private static Pt3ModuleMetadata ExtractMetadata(ReadOnlySpan<byte> data, Pt3HeaderKind headerKind)
    {
        return headerKind switch
        {
            Pt3HeaderKind.Standard => new Pt3ModuleMetadata(
                Version: ExtractAsciiTrimmed(data.Slice(13, 1)),
                Title: ExtractAsciiTrimmed(data.Slice(30, 32)),
                Author: ExtractAsciiTrimmed(data.Slice(66, 32))),
            Pt3HeaderKind.VortexTracker => new Pt3ModuleMetadata(
                Version: "7",
                Title: ExtractAsciiTrimmed(data.Slice(30, 32)),
                Author: ExtractAsciiTrimmed(data.Slice(66, 32))),
            _ => throw new Pt3FormatException("The PT3 header kind is unsupported."),
        };
    }

    private static Pt3HeaderKind DetectHeaderKind(ReadOnlySpan<byte> data)
    {
        if (data.Length >= StandardSignature.Length && data[..StandardSignature.Length].SequenceEqual(StandardSignature))
        {
            return Pt3HeaderKind.Standard;
        }

        if (data.Length >= VortexTrackerSignature.Length && data[..VortexTrackerSignature.Length].SequenceEqual(VortexTrackerSignature))
        {
            return Pt3HeaderKind.VortexTracker;
        }

        return Pt3HeaderKind.Unknown;
    }

    private enum Pt3HeaderKind
    {
        Unknown = 0,
        Standard,
        VortexTracker,
    }

    private static void EnsureReadable(Stream source)
    {
        if (!source.CanRead)
        {
            throw new ArgumentException("The source stream must be readable.", nameof(source));
        }
    }
}
