// SPDX-FileCopyrightText: 2026 Anton Shirokov
// SPDX-License-Identifier: MIT

namespace RetroSound.Core.Formats.Pt3;

/// <summary>
/// Describes one PT3 module candidate discovered inside a PT3 TurboSound file.
/// </summary>
public sealed class Pt3TurboSoundModuleCandidateInfo
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Pt3TurboSoundModuleCandidateInfo"/> class.
    /// </summary>
    /// <param name="offset">The byte offset where the candidate PT3 header starts.</param>
    /// <param name="headerKind">The header signature detected for the candidate.</param>
    /// <param name="parsedSuccessfully">Indicates whether the candidate could be parsed as a PT3 module.</param>
    /// <param name="usage">Describes how the candidate is used by playback.</param>
    /// <param name="metadata">The parsed PT3 metadata when parsing succeeded.</param>
    /// <param name="frequencyTable">The parsed PT3 frequency table when parsing succeeded.</param>
    /// <param name="tempo">The parsed PT3 tempo when parsing succeeded.</param>
    /// <param name="failureReason">The parse failure reason when parsing failed.</param>
    public Pt3TurboSoundModuleCandidateInfo(
        int offset,
        string headerKind,
        bool parsedSuccessfully,
        string usage,
        Pt3ModuleMetadata? metadata,
        Pt3FrequencyTableKind? frequencyTable,
        int? tempo,
        string? failureReason)
    {
        Offset = offset;
        HeaderKind = headerKind ?? throw new ArgumentNullException(nameof(headerKind));
        ParsedSuccessfully = parsedSuccessfully;
        Usage = usage ?? throw new ArgumentNullException(nameof(usage));
        Metadata = metadata;
        FrequencyTable = frequencyTable;
        Tempo = tempo;
        FailureReason = failureReason;
    }

    /// <summary>
    /// Gets the byte offset where the candidate PT3 header starts.
    /// </summary>
    public int Offset { get; }

    /// <summary>
    /// Gets the detected header signature kind.
    /// </summary>
    public string HeaderKind { get; }

    /// <summary>
    /// Gets a value indicating whether the candidate could be parsed successfully.
    /// </summary>
    public bool ParsedSuccessfully { get; }

    /// <summary>
    /// Gets how the candidate is used or skipped during playback selection.
    /// </summary>
    public string Usage { get; }

    /// <summary>
    /// Gets the parsed PT3 metadata when available.
    /// </summary>
    public Pt3ModuleMetadata? Metadata { get; }

    /// <summary>
    /// Gets the parsed PT3 frequency table when available.
    /// </summary>
    public Pt3FrequencyTableKind? FrequencyTable { get; }

    /// <summary>
    /// Gets the parsed PT3 tempo when available.
    /// </summary>
    public int? Tempo { get; }

    /// <summary>
    /// Gets the parse failure reason when the candidate is skipped as invalid.
    /// </summary>
    public string? FailureReason { get; }
}