// SPDX-FileCopyrightText: 2026 Anton Shirokov
// SPDX-License-Identifier: MIT

namespace RetroSound.Core.Formats.TurboSound;

/// <summary>
/// Identifies the high-level TurboSound-related binary shape detected from input bytes.
/// </summary>
public enum TurboSoundInputKind
{
    /// <summary>
    /// The input does not match a known TurboSound-related signature.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// The input starts with the current TS container signature.
    /// </summary>
    TsContainer,

    /// <summary>
    /// The input starts with a standalone PT3 module signature.
    /// </summary>
    Pt3Module,

    /// <summary>
    /// The input starts with a PT3 module and also contains a second embedded PT3 module for TurboSound playback.
    /// </summary>
    Pt3TurboSoundModule,
}