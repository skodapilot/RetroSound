// SPDX-FileCopyrightText: 2026 Anton Shirokov
// SPDX-License-Identifier: MIT

namespace RetroSound.Core.Formats.Pt3;

/// <summary>
/// Represents a parsed PT3 TurboSound module together with load-time diagnostics.
/// </summary>
public sealed class Pt3TurboSoundLoadResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Pt3TurboSoundLoadResult"/> class.
    /// </summary>
    /// <param name="module">The parsed PT3 TurboSound module.</param>
    /// <param name="diagnostics">The associated load diagnostics.</param>
    public Pt3TurboSoundLoadResult(Pt3TurboSoundModule module, Pt3TurboSoundLoadDiagnostics diagnostics)
    {
        Module = module ?? throw new ArgumentNullException(nameof(module));
        Diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
    }

    /// <summary>
    /// Gets the parsed PT3 TurboSound module.
    /// </summary>
    public Pt3TurboSoundModule Module { get; }

    /// <summary>
    /// Gets the associated load diagnostics.
    /// </summary>
    public Pt3TurboSoundLoadDiagnostics Diagnostics { get; }
}