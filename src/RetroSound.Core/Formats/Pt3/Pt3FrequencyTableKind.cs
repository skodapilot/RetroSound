// SPDX-FileCopyrightText: 2026 Anton Shirokov
// SPDX-License-Identifier: MIT

namespace RetroSound.Core.Formats.Pt3;

/// <summary>
/// Identifies the tone table declared by a PT3 module header.
/// </summary>
public enum Pt3FrequencyTableKind : byte
{
    /// <summary>
    /// The Pro Tracker tone table.
    /// </summary>
    ProTracker = 0,

    /// <summary>
    /// The Sound Tracker tone table.
    /// </summary>
    SoundTracker = 1,

    /// <summary>
    /// The ASM or PSC tone table.
    /// </summary>
    AsmOrPsc = 2,

    /// <summary>
    /// The RealSound tone table.
    /// </summary>
    RealSound = 3,
}