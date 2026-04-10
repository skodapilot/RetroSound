// SPDX-FileCopyrightText: 2026 Anton Shirokov
// SPDX-License-Identifier: MIT

namespace RetroSound.Core.Formats.Pt3;

/// <summary>
/// Describes how a PT3 TurboSound file was analyzed and which embedded modules were selected for playback.
/// </summary>
public sealed class Pt3TurboSoundLoadDiagnostics
{
    private readonly IReadOnlyList<Pt3TurboSoundModuleCandidateInfo> _candidates;

    /// <summary>
    /// Initializes a new instance of the <see cref="Pt3TurboSoundLoadDiagnostics"/> class.
    /// </summary>
    /// <param name="fileLength">The total file length in bytes.</param>
    /// <param name="candidates">The discovered PT3 module candidates.</param>
    public Pt3TurboSoundLoadDiagnostics(
        int fileLength,
        IReadOnlyList<Pt3TurboSoundModuleCandidateInfo> candidates)
    {
        FileLength = fileLength;
        _candidates = candidates ?? throw new ArgumentNullException(nameof(candidates));
    }

    /// <summary>
    /// Gets the total file length in bytes.
    /// </summary>
    public int FileLength { get; }

    /// <summary>
    /// Gets the discovered PT3 module candidates in file order.
    /// </summary>
    public IReadOnlyList<Pt3TurboSoundModuleCandidateInfo> Candidates => _candidates;

    /// <summary>
    /// Gets the number of discovered PT3 signatures, including invalid candidates.
    /// </summary>
    public int DiscoveredModuleCount => _candidates.Count;

    /// <summary>
    /// Gets the number of candidates that parsed successfully as PT3 modules.
    /// </summary>
    public int ParsedModuleCount => _candidates.Count(candidate => candidate.ParsedSuccessfully);

    /// <summary>
    /// Gets the number of parsed modules selected for playback.
    /// </summary>
    public int UsedModuleCount => _candidates.Count(candidate => candidate.Usage.StartsWith("Used", StringComparison.Ordinal));

    /// <summary>
    /// Gets the number of candidates that were skipped during selection.
    /// </summary>
    public int SkippedModuleCount => _candidates.Count(candidate => !candidate.Usage.StartsWith("Used", StringComparison.Ordinal));
}