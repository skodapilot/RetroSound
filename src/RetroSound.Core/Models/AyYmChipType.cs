// SPDX-FileCopyrightText: 2026 Anton Shirokov
// SPDX-License-Identifier: MIT

namespace RetroSound.Core.Models;

/// <summary>
/// Identifies the AY/YM chip family used to select the correct DAC curve during rendering.
/// </summary>
public enum AyYmChipType
{
    /// <summary>
    /// Uses the AY-3-8910 family DAC model.
    /// </summary>
    Ay38910 = 0,

    /// <summary>
    /// Uses the YM2149 family DAC model.
    /// </summary>
    Ym2149 = 1,
}