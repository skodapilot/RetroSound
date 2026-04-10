// SPDX-FileCopyrightText: 2026 Anton Shirokov
// SPDX-License-Identifier: MIT

namespace RetroSound.Core.Formats.Pt3;

/// <summary>
/// Represents a TurboSound PT3 file that embeds one PT3 module per AY/YM chip.
/// </summary>
public sealed class Pt3TurboSoundModule
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Pt3TurboSoundModule"/> class.
    /// </summary>
    /// <param name="firstChip">The PT3 module played by the first AY/YM chip.</param>
    /// <param name="secondChip">The PT3 module played by the second AY/YM chip.</param>
    /// <param name="title">The optional title exposed for the combined TurboSound module.</param>
    /// <param name="diagnostics">The diagnostics captured while locating the embedded PT3 modules.</param>
    public Pt3TurboSoundModule(Pt3Module firstChip, Pt3Module secondChip, string? title, Pt3TurboSoundLoadDiagnostics diagnostics)
    {
        FirstChip = firstChip ?? throw new ArgumentNullException(nameof(firstChip));
        SecondChip = secondChip ?? throw new ArgumentNullException(nameof(secondChip));
        Title = title;
        Diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
    }

    /// <summary>
    /// Gets the PT3 module played by the first AY/YM chip.
    /// </summary>
    public Pt3Module FirstChip { get; }

    /// <summary>
    /// Gets the PT3 module played by the second AY/YM chip.
    /// </summary>
    public Pt3Module SecondChip { get; }

    /// <summary>
    /// Gets the optional title exposed for the combined TurboSound module.
    /// </summary>
    public string? Title { get; }

    /// <summary>
    /// Gets the diagnostics captured while locating the embedded PT3 modules.
    /// </summary>
    public Pt3TurboSoundLoadDiagnostics Diagnostics { get; }
}